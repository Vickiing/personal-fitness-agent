using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.Core.Application;

public sealed record ExerciseTrend(Guid ExerciseId, int ExerciseVersion, decimal? BestLoadKg, int? RepetitionsAtBestLoad,
    decimal? VolumeProxyKgReps, int ObservationCount);

public sealed record WeeklySummary(DateOnly StartDate, DateOnly EndDate, decimal? AdherencePercent,
    decimal? MedianSleepHours, decimal? MedianWeightKg, decimal? MedianWaistCm,
    int WorkoutCount, int SymptomCount, IReadOnlyList<ExerciseTrend> ExerciseTrends,
    IReadOnlyList<string> DataQualityWarnings, string Provenance,
    decimal? WeightChangeKg28Days, int EarlierWeightCount, int RecentWeightCount);

/// <summary>Deterministic summaries only; sparse data remains unknown and energy is not inferred here.</summary>
public static class WeeklyAnalytics
{
    public static WeeklySummary Summarize(DateOnly endDate, IEnumerable<WorkoutSession> sessions,
        IEnumerable<DailyLog> dailyLogs, WorkoutSchedule? schedule)
    {
        var start = endDate.AddDays(-6);
        var planned = schedule?.CountPlannedSessions(start, endDate) ?? 0;
        return Summarize(endDate, sessions, dailyLogs, planned);
    }

    public static WeeklySummary Summarize(DateOnly endDate, IEnumerable<WorkoutSession> sessions,
        IEnumerable<DailyLog> dailyLogs, int plannedSessionsInWindow)
    {
        if (plannedSessionsInWindow < 0) throw new ArgumentOutOfRangeException(nameof(plannedSessionsInWindow));
        var start = endDate.AddDays(-6);
        var weekSessions = sessions.Where(s => DateOnly.FromDateTime(s.StartedAt.AtOriginalOffset.DateTime) >= start &&
            DateOnly.FromDateTime(s.StartedAt.AtOriginalOffset.DateTime) <= endDate && s.Status == WorkoutSessionStatus.Completed).ToArray();
        var allLogs = dailyLogs.ToArray();
        var logs = allLogs.Where(log => log.LocalDate >= start && log.LocalDate <= endDate).ToArray();
        var weights = logs.Where(log => log.WeightKg is not null).Select(log => log.WeightKg!.Value).ToArray();
        var waists = logs.Where(log => log.WaistCm is not null).Select(log => log.WaistCm!.Value).ToArray();
        var sleep = logs.Where(log => log.SleepHours is not null).Select(log => log.SleepHours!.Value).ToArray();

        var trendStart = endDate.AddDays(-27);
        var recentStart = endDate.AddDays(-13);
        var earlierWeights = allLogs.Where(log => log.LocalDate >= trendStart && log.LocalDate < recentStart && log.WeightKg is not null)
            .Select(log => log.WeightKg!.Value).ToArray();
        var recentWeights = allLogs.Where(log => log.LocalDate >= recentStart && log.LocalDate <= endDate && log.WeightKg is not null)
            .Select(log => log.WeightKg!.Value).ToArray();
        decimal? weightChange = earlierWeights.Length >= 3 && recentWeights.Length >= 3
            ? Median(recentWeights) - Median(earlierWeights) : null;

        var warnings = new List<string>();
        if (weights.Length < 3) warnings.Add("Poucas medidas de peso para uma mediana semanal confiavel.");
        if (weightChange is null) warnings.Add("Dados insuficientes para comparar medianas de peso em 28 dias; sao necessarias 3 medidas em cada periodo de 14 dias.");
        if (sleep.Length < 3) warnings.Add("Poucos registros de sono nesta semana.");
        if (plannedSessionsInWindow == 0) warnings.Add("Adesao indisponivel: nenhum treino planejado informado.");
        decimal? adherence = plannedSessionsInWindow == 0 ? null : Math.Min(100m, weekSessions.Length * 100m / plannedSessionsInWindow);
        var exerciseTrends = weekSessions.SelectMany(session => session.Sets).GroupBy(set => (set.ExerciseId, set.ExerciseVersion)).Select(group =>
        {
            var loaded = group.Where(set => set.LoadKg is not null && set.Repetitions is not null).ToArray();
            var bestLoad = loaded.Select(set => set.LoadKg!.Value).DefaultIfEmpty().Max();
            var bestSets = loaded.Where(set => set.LoadKg == bestLoad).ToArray();
            var volume = loaded.Sum(set => set.LoadKg!.Value * set.Repetitions!.Value);
            return new ExerciseTrend(group.Key.ExerciseId, group.Key.ExerciseVersion, loaded.Length == 0 ? null : bestLoad,
                bestSets.Length == 0 ? null : bestSets.Max(set => set.Repetitions!.Value),
                loaded.Length == 0 ? null : volume, loaded.Length);
        }).ToArray();
        return new WeeklySummary(start, endDate, adherence, Median(sleep), Median(weights), Median(waists),
            weekSessions.Length, logs.Sum(log => log.Symptoms.Count), exerciseTrends, warnings,
            "Sessions: recorded events; body measurements and sleep: self-reported or measured at capture.",
            weightChange, earlierWeights.Length, recentWeights.Length);
    }

    private static decimal? Median(decimal[] values)
    {
        if (values.Length == 0) return null;
        Array.Sort(values);
        var middle = values.Length / 2;
        return values.Length % 2 == 0 ? (values[middle - 1] + values[middle]) / 2m : values[middle];
    }
}
