namespace PersonalFitnessAgent.Core.Domain;

/// <summary>A locally imported, read-only workout sheet. Content is user supplied data, never executable instructions.</summary>
public sealed record WorkoutSheet
{
    public Guid Id { get; init; }
    public DateTimeOffset ImportedAtUtc { get; init; }
    public IReadOnlyList<WorkoutSheetDay> Days { get; init; }

    public WorkoutSheet(Guid id, DateTimeOffset importedAtUtc, IReadOnlyList<WorkoutSheetDay> days)
    {
        if (id == Guid.Empty) throw new ArgumentException("Workout sheet ID is required.", nameof(id));
        if (importedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Import time must be UTC.", nameof(importedAtUtc));
        ArgumentNullException.ThrowIfNull(days);
        if (days.Count == 0) throw new ArgumentException("At least one workout day is required.", nameof(days));
        Id = id;
        ImportedAtUtc = importedAtUtc;
        Days = days;
    }
}

public sealed record WorkoutSheetDay
{
    public string Key { get; init; }
    public string Title { get; init; }
    public string Summary { get; init; }
    public IReadOnlyList<WorkoutSheetExercise> Exercises { get; init; }
    public IReadOnlyList<string> Notes { get; init; }

    public WorkoutSheetDay(string key, string title, string summary, IReadOnlyList<WorkoutSheetExercise> exercises,
        IReadOnlyList<string> notes)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Workout day key and title are required.");
        ArgumentNullException.ThrowIfNull(exercises);
        ArgumentNullException.ThrowIfNull(notes);
        if (exercises.Count == 0) throw new ArgumentException("A workout day must contain at least one exercise.", nameof(exercises));
        Key = key;
        Title = title;
        Summary = summary ?? "";
        Exercises = exercises;
        Notes = notes;
    }
}

public sealed record WorkoutSheetExercise
{
    public int Order { get; init; }
    public string Name { get; init; }
    public string Sets { get; init; }
    public string Repetitions { get; init; }
    public string Rest { get; init; }
    public string Instructions { get; init; }
    public string Priority { get; init; }
    public string ReferenceLoad { get; init; }

    public WorkoutSheetExercise(int order, string name, string sets, string repetitions, string rest,
        string instructions, string priority, string referenceLoad)
    {
        if (order < 1) throw new ArgumentOutOfRangeException(nameof(order));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Exercise name is required.", nameof(name));
        Order = order;
        Name = name;
        Sets = sets ?? "";
        Repetitions = repetitions ?? "";
        Rest = rest ?? "";
        Instructions = instructions ?? "";
        Priority = priority ?? "";
        ReferenceLoad = referenceLoad ?? "";
    }
}
