using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Domain;

public enum RecommendationStatus { Pending, Approved, Rejected, Suspended }

public sealed record Recommendation
{
    public Guid Id { get; }
    public string RuleId { get; }
    public int RuleVersion { get; }
    public IReadOnlyList<Guid> SourceEventIds { get; }
    public string Rationale { get; }
    public string Quality { get; }
    public string ProposedChange { get; }
    public IReadOnlySet<string> TargetMovementPatterns { get; }
    public RecordedTimestamp CreatedAt { get; }
    public RecommendationStatus Status { get; }
    public string? DecisionReason { get; }
    public DateTimeOffset? DecidedAtUtc { get; }
    public RestrictionEvaluation RestrictionTrace { get; }

    public Recommendation(Guid id, string ruleId, int ruleVersion, IEnumerable<Guid> sourceEventIds, string rationale,
        string quality, string proposedChange, RecordedTimestamp createdAt, RestrictionEvaluation restrictionTrace,
        IEnumerable<string>? targetMovementPatterns = null,
        RecommendationStatus status = RecommendationStatus.Pending, string? decisionReason = null, DateTimeOffset? decidedAtUtc = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Recommendation ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(ruleId) || string.IsNullOrWhiteSpace(rationale) || string.IsNullOrWhiteSpace(proposedChange))
            throw new ArgumentException("Rule, rationale and proposed change are required.");
        if (ruleVersion < 1) throw new ArgumentOutOfRangeException(nameof(ruleVersion));
        if (decidedAtUtc is { Offset: var offset } && offset != TimeSpan.Zero) throw new ArgumentException("Decision timestamp must be UTC.", nameof(decidedAtUtc));
        Id = id; RuleId = ruleId; RuleVersion = ruleVersion; SourceEventIds = sourceEventIds.Distinct().ToArray();
        Rationale = rationale; Quality = quality; ProposedChange = proposedChange; CreatedAt = createdAt;
        TargetMovementPatterns = new HashSet<string>(targetMovementPatterns ?? [], StringComparer.OrdinalIgnoreCase);
        RestrictionTrace = restrictionTrace; Status = status; DecisionReason = decisionReason; DecidedAtUtc = decidedAtUtc;
    }

    public Recommendation Decide(bool approve, string reason, DateTimeOffset decidedAtUtc)
    {
        if (Status != RecommendationStatus.Pending) throw new InvalidOperationException("Only pending recommendations can be decided.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A decision reason is required.", nameof(reason));
        if (decidedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Decision timestamp must be UTC.", nameof(decidedAtUtc));
        if (approve && RestrictionTrace.Decision is RestrictionDecision.Block or RestrictionDecision.RequiresReview)
            throw new InvalidOperationException("This recommendation requires restriction review before approval.");
        return new Recommendation(Id, RuleId, RuleVersion, SourceEventIds, Rationale, Quality, ProposedChange, CreatedAt,
            RestrictionTrace, TargetMovementPatterns, approve ? RecommendationStatus.Approved : RecommendationStatus.Rejected, reason, decidedAtUtc);
    }

    public Recommendation Suspend(string reason, DateTimeOffset atUtc)
    {
        if (Status != RecommendationStatus.Pending) return this;
        if (atUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Suspension timestamp must be UTC.", nameof(atUtc));
        return new Recommendation(Id, RuleId, RuleVersion, SourceEventIds, Rationale, Quality, ProposedChange, CreatedAt,
            RestrictionTrace, TargetMovementPatterns, RecommendationStatus.Suspended, reason, atUtc);
    }
}

public static class DelayedSymptomReview
{
    public static IReadOnlyList<Recommendation> SuspendAffected(IEnumerable<Recommendation> recommendations,
        SymptomEvent symptom, DateTimeOffset reviewedAtUtc)
    {
        if (reviewedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Review timestamp must be UTC.", nameof(reviewedAtUtc));
        return recommendations.Select(recommendation =>
        {
            var affectedByPattern = symptom.MovementPatterns.Count == 0 ||
                recommendation.TargetMovementPatterns.Overlaps(symptom.MovementPatterns);
            var affectedBySession = symptom.RelatedSessionId is not null && recommendation.SourceEventIds.Contains(symptom.RelatedSessionId.Value);
            return affectedByPattern || affectedBySession
                ? recommendation.Suspend("Delayed symptom reported; human review required.", reviewedAtUtc)
                : recommendation;
        }).ToArray();
    }
}
