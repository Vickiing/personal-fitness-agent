using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Domain;

public sealed record WorkoutPlan
{
    public Guid Id { get; }
    public int Version { get; }
    public string Label { get; }
    public IReadOnlyList<ExercisePrescription> Exercises { get; }

    public WorkoutPlan(Guid id, int version, string label, IEnumerable<ExercisePrescription> exercises)
    {
        if (id == Guid.Empty) throw new ArgumentException("Plan ID is required.", nameof(id));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Plan label is required.", nameof(label));
        Id = id;
        Version = version;
        Label = label.Trim();
        Exercises = exercises?.ToArray() ?? throw new ArgumentNullException(nameof(exercises));
    }
}

public sealed record ExerciseSet
{
    public Guid Id { get; init; }
    public Guid ExerciseId { get; init; }
    public int ExerciseVersion { get; init; }
    public int SetIndex { get; init; }
    public decimal? LoadKg { get; init; }
    public int? Repetitions { get; init; }
    public decimal? RepetitionsInReserve { get; init; }
    public string? Notes { get; init; }

    public ExerciseSet(Guid id, Guid exerciseId, int exerciseVersion, int setIndex, decimal? loadKg, int? repetitions, decimal? repetitionsInReserve = null, string? notes = null)
    {
        if (id == Guid.Empty || exerciseId == Guid.Empty) throw new ArgumentException("Set and exercise IDs are required.");
        if (exerciseVersion < 1) throw new ArgumentOutOfRangeException(nameof(exerciseVersion));
        if (setIndex < 1) throw new ArgumentOutOfRangeException(nameof(setIndex));
        if (loadKg < 0) throw new ArgumentOutOfRangeException(nameof(loadKg));
        if (repetitions < 0) throw new ArgumentOutOfRangeException(nameof(repetitions));
        if (repetitionsInReserve < 0) throw new ArgumentOutOfRangeException(nameof(repetitionsInReserve));
        Id = id; ExerciseId = exerciseId; ExerciseVersion = exerciseVersion; SetIndex = setIndex;
        LoadKg = loadKg; Repetitions = repetitions; RepetitionsInReserve = repetitionsInReserve; Notes = notes;
    }
}

public enum WorkoutSessionStatus { InProgress, Completed, Cancelled }

public sealed record WorkoutSession
{
    public Guid Id { get; }
    public Guid PlanId { get; }
    public int PlanVersion { get; }
    public RecordedTimestamp StartedAt { get; }
    public RecordedTimestamp? CompletedAt { get; }
    public WorkoutSessionStatus Status { get; }
    public TimeSpan? Duration { get; }
    public string? Notes { get; }
    public IReadOnlyList<ExerciseSet> Sets { get; }

    public WorkoutSession(Guid id, Guid planId, int planVersion, RecordedTimestamp startedAt, IEnumerable<ExerciseSet>? sets = null,
        WorkoutSessionStatus status = WorkoutSessionStatus.InProgress, RecordedTimestamp? completedAt = null, TimeSpan? duration = null, string? notes = null)
    {
        if (id == Guid.Empty || planId == Guid.Empty) throw new ArgumentException("Session and plan IDs are required.");
        if (planVersion < 1) throw new ArgumentOutOfRangeException(nameof(planVersion));
        if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        if (status == WorkoutSessionStatus.Completed && completedAt is null) throw new ArgumentException("Completed sessions require a completion timestamp.", nameof(completedAt));
        if (completedAt is not null && completedAt.Value.Utc < startedAt.Utc) throw new ArgumentException("Completion cannot precede the session start.", nameof(completedAt));
        Id = id; PlanId = planId; PlanVersion = planVersion; StartedAt = startedAt; Sets = sets?.ToArray() ?? [];
        Status = status; CompletedAt = completedAt; Duration = duration; Notes = notes;
    }
}

public static class PlanAlternation
{
    public static WorkoutPlan? Next(IReadOnlyList<WorkoutPlan> plans, IReadOnlyList<WorkoutSession> sessions)
    {
        var candidates = plans.Where(p => p.Exercises.Count > 0).OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase).ToArray();
        if (candidates.Length == 0) return null;
        var lastPlan = sessions.Where(s => s.Status == WorkoutSessionStatus.Completed)
            .OrderByDescending(s => s.CompletedAt?.Utc ?? s.StartedAt.Utc).FirstOrDefault();
        if (lastPlan is null) return candidates[0];
        var previous = Array.FindIndex(candidates, p => p.Id == lastPlan.PlanId);
        return candidates[(previous + 1 + candidates.Length) % candidates.Length];
    }
}
