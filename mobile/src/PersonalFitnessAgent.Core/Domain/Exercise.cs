namespace PersonalFitnessAgent.Core.Domain;

public sealed record Exercise
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int Version { get; init; }
    public IReadOnlySet<string> MovementPatterns { get; init; }
    public IReadOnlySet<string> Equipment { get; init; }
    public bool? UsesExternalLoad { get; init; }
    public string? Region { get; init; }
    public string? Position { get; init; }
    public string? RangeOfMotion { get; init; }

    public Exercise(Guid id, string name, int version, IEnumerable<string>? movementPatterns = null, IEnumerable<string>? equipment = null,
        bool? usesExternalLoad = null, string? region = null, string? position = null, string? rangeOfMotion = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Exercise ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Exercise name is required.", nameof(name));
        if (version < 1) throw new ArgumentOutOfRangeException(nameof(version));
        Id = id;
        Name = name.Trim();
        Version = version;
        MovementPatterns = new HashSet<string>(movementPatterns ?? [], StringComparer.OrdinalIgnoreCase);
        Equipment = new HashSet<string>(equipment ?? [], StringComparer.OrdinalIgnoreCase);
        UsesExternalLoad = usesExternalLoad;
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim();
        RangeOfMotion = string.IsNullOrWhiteSpace(rangeOfMotion) ? null : rangeOfMotion.Trim();
    }
}

public sealed record ExercisePrescription
{
    public Guid ExerciseId { get; }
    public int ExerciseVersion { get; }
    public int? TargetSets { get; }
    public int? TargetRepetitions { get; }

    public ExercisePrescription(Guid exerciseId, int exerciseVersion, int? targetSets = null, int? targetRepetitions = null)
    {
        if (exerciseId == Guid.Empty) throw new ArgumentException("Exercise ID is required.", nameof(exerciseId));
        if (exerciseVersion < 1) throw new ArgumentOutOfRangeException(nameof(exerciseVersion));
        if (targetSets is < 1) throw new ArgumentOutOfRangeException(nameof(targetSets));
        if (targetRepetitions is < 0) throw new ArgumentOutOfRangeException(nameof(targetRepetitions));
        ExerciseId = exerciseId;
        ExerciseVersion = exerciseVersion;
        TargetSets = targetSets;
        TargetRepetitions = targetRepetitions;
    }
}
