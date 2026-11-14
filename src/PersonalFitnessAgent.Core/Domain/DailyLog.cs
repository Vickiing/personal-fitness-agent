using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Domain;

public enum QuantitySource { Measured, SelfReported, Estimated }
public enum ActivityLevel { Unknown, Sedentary, Light, Moderate, High, VeryHigh }

public sealed record Quantity
{
    public decimal Value { get; }
    public string Unit { get; }
    public QuantitySource Source { get; }
    public decimal? Minimum { get; }
    public decimal? Maximum { get; }

    public Quantity(decimal value, string unit, QuantitySource source, decimal? minimum = null, decimal? maximum = null)
    {
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (value < 0 || minimum < 0 || maximum < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (minimum is not null && maximum is not null && minimum > maximum) throw new ArgumentException("Minimum cannot exceed maximum.");
        if (minimum is decimal min && value < min || maximum is decimal max && value > max)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be inside the declared range.");
        if (!Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(source));
        Value = value; Unit = unit; Source = source; Minimum = minimum; Maximum = maximum;
    }
}

public sealed record FoodEntry
{
    public Guid Id { get; }
    public string Name { get; }
    public string? PortionDescription { get; }
    public Quantity? Amount { get; }
    public Quantity? EnergyKcal { get; }
    public bool IsQuantified { get; }

    public FoodEntry(Guid id, string name, string? portionDescription, Quantity? amount, Quantity? energyKcal, bool isQuantified)
    {
        if (id == Guid.Empty) throw new ArgumentException("Food entry ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Food name is required.", nameof(name));
        if (!isQuantified && energyKcal is not null) throw new ArgumentException("Unquantified food cannot claim an energy value.", nameof(energyKcal));
        if (energyKcal is not null && energyKcal.Unit != "kcal") throw new ArgumentException("Food energy must use kcal.", nameof(energyKcal));
        Id = id; Name = name.Trim(); PortionDescription = portionDescription; Amount = amount; EnergyKcal = energyKcal; IsQuantified = isQuantified;
    }
}

public sealed record SymptomEvent
{
    public Guid Id { get; }
    public RecordedTimestamp RecordedAt { get; }
    public DateTimeOffset? OnsetUtc { get; }
    public string Region { get; }
    public int? Severity { get; }
    public string Description { get; }
    public Guid? RelatedSessionId { get; }
    public IReadOnlySet<string> MovementPatterns { get; }

    public SymptomEvent(Guid id, RecordedTimestamp recordedAt, DateTimeOffset? onsetUtc, string region, int? severity,
        string description, Guid? relatedSessionId = null, IEnumerable<string>? movementPatterns = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Symptom event ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Region and description are required.");
        if (severity is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(severity));
        if (onsetUtc is { Offset: var offset } && offset != TimeSpan.Zero) throw new ArgumentException("Onset must be UTC.", nameof(onsetUtc));
        if (onsetUtc is { } onset && onset > recordedAt.Utc) throw new ArgumentException("Symptom onset cannot be in the future relative to its recording.", nameof(onsetUtc));
        Id = id; RecordedAt = recordedAt; OnsetUtc = onsetUtc; Region = region.Trim(); Severity = severity;
        Description = description.Trim(); RelatedSessionId = relatedSessionId;
        MovementPatterns = new HashSet<string>(movementPatterns ?? [], StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record DailyLog
{
    public Guid Id { get; }
    public DateOnly LocalDate { get; }
    public decimal? SleepHours { get; }
    public ActivityLevel Activity { get; }
    public IReadOnlyList<FoodEntry> Foods { get; }
    public Quantity? WeightKg { get; }
    public Quantity? WaistCm { get; }
    public IReadOnlyList<SymptomEvent> Symptoms { get; }

    public DailyLog(Guid id, DateOnly localDate, decimal? sleepHours = null, ActivityLevel activity = ActivityLevel.Unknown,
        IEnumerable<FoodEntry>? foods = null, Quantity? weightKg = null, Quantity? waistCm = null, IEnumerable<SymptomEvent>? symptoms = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Daily log ID is required.", nameof(id));
        if (sleepHours < 0 || sleepHours > 24) throw new ArgumentOutOfRangeException(nameof(sleepHours));
        if (weightKg is not null && weightKg.Unit != "kg") throw new ArgumentException("Weight must be stored in kg.", nameof(weightKg));
        if (waistCm is not null && waistCm.Unit != "cm") throw new ArgumentException("Waist must be stored in cm.", nameof(waistCm));
        Id = id; LocalDate = localDate; SleepHours = sleepHours; Activity = activity;
        Foods = foods?.ToArray() ?? []; WeightKg = weightKg; WaistCm = waistCm; Symptoms = symptoms?.ToArray() ?? [];
    }
}
