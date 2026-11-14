namespace PersonalFitnessAgent.Core.Domain;

/// <summary>A user's planned workout weekdays. Empty schedules represent no configured plan.</summary>
public sealed record WorkoutSchedule
{
    public IReadOnlySet<DayOfWeek> PlannedWeekdays { get; }

    public WorkoutSchedule(IEnumerable<DayOfWeek> plannedWeekdays)
    {
        ArgumentNullException.ThrowIfNull(plannedWeekdays);
        var days = plannedWeekdays.ToHashSet();
        if (days.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentOutOfRangeException(nameof(plannedWeekdays));
        PlannedWeekdays = days;
    }

    public int CountPlannedSessions(DateOnly startInclusive, DateOnly endInclusive)
    {
        if (endInclusive < startInclusive) throw new ArgumentOutOfRangeException(nameof(endInclusive));
        var count = 0;
        for (var date = startInclusive; date <= endInclusive; date = date.AddDays(1))
            if (PlannedWeekdays.Contains(date.DayOfWeek)) count++;
        return count;
    }
}
