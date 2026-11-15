namespace PersonalFitnessAgent.Core.Domain;

public enum RestrictionAction { AllowWithReview, Warn, Block }
public enum RestrictionSource { UserReport, Clinician, Other }
public enum RestrictionStatus { Active, PendingReview, Retired }

public sealed record RestrictionPolicy
{
    public Guid Id { get; }
    public string Label { get; }
    public string? Region { get; }
    public string? MovementPattern { get; }
    public string? Equipment { get; }
    public bool? ExternalLoad { get; }
    public string? Position { get; }
    public string? RangeOfMotion { get; }
    public RestrictionAction Action { get; }
    public RestrictionSource Source { get; }
    public RestrictionStatus Status { get; }
    public string Reason { get; }
    public int Version { get; }
    public DateOnly? ReviewOn { get; }

    public RestrictionPolicy(Guid id, string label, RestrictionAction action, RestrictionSource source, string reason,
        RestrictionStatus status = RestrictionStatus.Active, string? region = null, string? movementPattern = null,
        string? equipment = null, bool? externalLoad = null, string? position = null, string? rangeOfMotion = null,
        int version = 1, DateOnly? reviewOn = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Policy ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Label and reason are required.");
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        if (!Enum.IsDefined(action) || !Enum.IsDefined(source) || !Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(action));
        if (movementPattern is null && region is null && equipment is null && externalLoad is null && position is null && rangeOfMotion is null)
            throw new ArgumentException("A policy must define at least one scope trigger.");
        Id = id; Label = label.Trim(); Action = action; Source = source; Reason = reason.Trim(); Status = status;
        Region = region; MovementPattern = movementPattern; Equipment = equipment; ExternalLoad = externalLoad;
        Position = position; RangeOfMotion = rangeOfMotion; Version = version; ReviewOn = reviewOn;
    }
}

public sealed record ExerciseContext(Guid ExerciseId, string Name, IReadOnlySet<string>? MovementPatterns,
    IReadOnlySet<string>? Equipment, bool? UsesExternalLoad, string? Position = null, string? RangeOfMotion = null,
    string? Region = null);

public enum RestrictionDecision { NoPolicyMatched, Warn, RequiresReview, Block }

public sealed record RestrictionEvaluation(RestrictionDecision Decision, IReadOnlyList<Guid> PolicyIds,
    IReadOnlyList<string> Reasons, IReadOnlyList<string> Conflicts, DateTimeOffset EvaluatedAtUtc);

public static class RestrictionEngine
{
    public static RestrictionEvaluation Evaluate(ExerciseContext? exercise, IEnumerable<RestrictionPolicy> policies, DateTimeOffset evaluatedAtUtc)
    {
        if (evaluatedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Evaluation time must be UTC.", nameof(evaluatedAtUtc));
        var active = policies.Where(p => p.Status != RestrictionStatus.Retired).ToArray();
        if (exercise is null || exercise.ExerciseId == Guid.Empty || string.IsNullOrWhiteSpace(exercise.Name))
            return Result(RestrictionDecision.RequiresReview, [], ["Exercise metadata is missing."], []);

        var matched = new List<RestrictionPolicy>();
        var unknown = new List<RestrictionPolicy>();
        foreach (var policy in active)
        {
            var match = Match(policy, exercise);
            if (match == MatchResult.Match) matched.Add(policy);
            else if (match == MatchResult.Unknown) unknown.Add(policy);
            // A pending policy only requires review for exercises in its scope, or when
            // missing metadata prevents us from ruling out a match.
            if (policy.Status == RestrictionStatus.PendingReview && match != MatchResult.NoMatch)
                unknown.Add(policy);
        }

        var conflicts = matched.Where(p => p.Source == RestrictionSource.UserReport).Any(user =>
            matched.Any(clinician => clinician.Source == RestrictionSource.Clinician && clinician.Action != user.Action))
            ? new[] { "User preference and clinician policy conflict; clinician review is required." }
            : Array.Empty<string>();
        var decision = matched.Any(p => p.Action == RestrictionAction.Block) ? RestrictionDecision.Block
            : unknown.Count > 0 || conflicts.Length > 0 || matched.Any(p => p.Action == RestrictionAction.AllowWithReview)
                ? RestrictionDecision.RequiresReview
                : matched.Any(p => p.Action == RestrictionAction.Warn) ? RestrictionDecision.Warn
                : RestrictionDecision.NoPolicyMatched;
        return Result(decision, matched.Concat(unknown).DistinctBy(p => p.Id).Select(p => p.Id).ToArray(),
            matched.Concat(unknown).DistinctBy(p => p.Id).Select(p => p.Reason).ToArray(), conflicts);

        RestrictionEvaluation Result(RestrictionDecision d, IReadOnlyList<Guid> ids, IReadOnlyList<string> reasons, IReadOnlyList<string> c) =>
            new(d, ids, reasons, c, evaluatedAtUtc);
    }

    private static MatchResult Match(RestrictionPolicy policy, ExerciseContext exercise)
    {
        var checkedAny = false;
        if (policy.MovementPattern is not null)
        {
            if (exercise.MovementPatterns is null) return MatchResult.Unknown;
            checkedAny = true;
            if (!exercise.MovementPatterns.Contains(policy.MovementPattern)) return MatchResult.NoMatch;
        }
        if (policy.Equipment is not null)
        {
            if (exercise.Equipment is null) return MatchResult.Unknown;
            checkedAny = true;
            if (!exercise.Equipment.Contains(policy.Equipment)) return MatchResult.NoMatch;
        }
        if (policy.ExternalLoad is not null)
        {
            if (exercise.UsesExternalLoad is null) return MatchResult.Unknown;
            checkedAny = true;
            if (exercise.UsesExternalLoad != policy.ExternalLoad) return MatchResult.NoMatch;
        }
        if (policy.Position is not null)
        {
            if (exercise.Position is null) return MatchResult.Unknown;
            checkedAny = true;
            if (!StringComparer.OrdinalIgnoreCase.Equals(exercise.Position, policy.Position)) return MatchResult.NoMatch;
        }
        if (policy.RangeOfMotion is not null)
        {
            if (exercise.RangeOfMotion is null) return MatchResult.Unknown;
            checkedAny = true;
            if (!StringComparer.OrdinalIgnoreCase.Equals(exercise.RangeOfMotion, policy.RangeOfMotion)) return MatchResult.NoMatch;
        }
        if (policy.Region is not null)
        {
            if (exercise.Region is null) return MatchResult.Unknown;
            checkedAny = true;
            if (!StringComparer.OrdinalIgnoreCase.Equals(exercise.Region, policy.Region)) return MatchResult.NoMatch;
        }
        return checkedAny ? MatchResult.Match : MatchResult.Unknown;
    }

    private enum MatchResult { NoMatch, Match, Unknown }
}
