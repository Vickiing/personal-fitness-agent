using PersonalFitnessAgent.Core.Application;
using PersonalFitnessAgent.Core.Domain;
using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Tests.Domain;

public class WorkoutAndAnalyticsTests
{
    private static readonly RecordedTimestamp Start = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(-3));

    [Fact]
    public void AlternatesByMostRecentlyCompletedPlan()
    {
        var exerciseId = Guid.NewGuid();
        var a = new WorkoutPlan(Guid.NewGuid(), 1, "A", [new ExercisePrescription(exerciseId, 1)]);
        var b = new WorkoutPlan(Guid.NewGuid(), 1, "B", [new ExercisePrescription(exerciseId, 1)]);
        var completed = new WorkoutSession(Guid.NewGuid(), a.Id, 1, Start, status: WorkoutSessionStatus.Completed,
            completedAt: new RecordedTimestamp(Start.Utc.AddHours(1), Start.OriginalOffset));

        Assert.Equal("B", PlanAlternation.Next([a, b], [completed])!.Label);
    }

    [Fact]
    public void UnknownFoodDoesNotInventEnergy()
    {
        Assert.Throws<ArgumentException>(() => new FoodEntry(Guid.NewGuid(), "doce", null, null,
            new Quantity(500, "kcal", QuantitySource.Estimated), false));
    }

    [Fact]
    public void FoodEnergyIsEstimatedOnlyWhenTheUserEntersGrams()
    {
        var rice = FoodCatalog.Items.Single(item => item.Code == "TACO-0003");

        Assert.Null(FoodCatalog.EstimateEnergy(rice, null));
        Assert.Equal(64m, FoodCatalog.EstimateEnergy(rice, 50m)!.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => FoodCatalog.EstimateEnergy(rice, -1m));
    }

    [Fact]
    public void SparseMeasurementsRemainUnknownAndWarn()
    {
        var log = new DailyLog(Guid.NewGuid(), new DateOnly(2026, 11, 14), sleepHours: 7);

        var result = WeeklyAnalytics.Summarize(new DateOnly(2026, 11, 14), [], [log], plannedSessionsInWindow: 0);

        Assert.Null(result.MedianWeightKg);
        Assert.Null(result.AdherencePercent);
        Assert.NotEmpty(result.DataQualityWarnings);
    }

    [Fact]
    public void ScheduledAdherenceCountsOnlyPlannedDaysThatHaveElapsed()
    {
        var planId = Guid.NewGuid();
        var schedule = new WorkoutSchedule([DayOfWeek.Friday, DayOfWeek.Sunday]);
        var completedFriday = new WorkoutSession(Guid.NewGuid(), planId, 1,
            new RecordedTimestamp(new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero), TimeSpan.Zero),
            status: WorkoutSessionStatus.Completed,
            completedAt: new RecordedTimestamp(new DateTimeOffset(2026, 11, 13, 11, 0, 0, TimeSpan.Zero), TimeSpan.Zero));

        var result = WeeklyAnalytics.Summarize(new DateOnly(2026, 11, 14), [completedFriday], [], schedule);

        Assert.Equal(50m, result.AdherencePercent);
        Assert.DoesNotContain(result.DataQualityWarnings, warning => warning.Contains("nenhum treino planejado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyScheduleKeepsAdherenceUnknown()
    {
        var result = WeeklyAnalytics.Summarize(new DateOnly(2026, 11, 14), [], [], new WorkoutSchedule([]));

        Assert.Null(result.AdherencePercent);
        Assert.Contains(result.DataQualityWarnings, warning => warning.Contains("nenhum treino planejado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScheduleRejectsInvalidWeekday()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkoutSchedule([(DayOfWeek)7]));
    }

    [Fact]
    public void TwentyEightDayWeightChangeUsesTwoFourteenDayMediansAndToleratesOutliers()
    {
        var logs = new[]
        {
            WeightLog(new DateOnly(2026, 10, 18), 70), WeightLog(new DateOnly(2026, 10, 20), 71),
            WeightLog(new DateOnly(2026, 10, 26), 72), WeightLog(new DateOnly(2026, 10, 31), 1000),
            WeightLog(new DateOnly(2026, 11, 1), 69), WeightLog(new DateOnly(2026, 11, 5), 70),
            WeightLog(new DateOnly(2026, 11, 9), 71), WeightLog(new DateOnly(2026, 11, 14), 1000)
        };

        var result = WeeklyAnalytics.Summarize(new DateOnly(2026, 11, 14), [], logs, plannedSessionsInWindow: 0);

        Assert.Equal(4, result.EarlierWeightCount);
        Assert.Equal(4, result.RecentWeightCount);
        Assert.Equal(-1m, result.WeightChangeKg28Days);
    }

    [Fact]
    public void TwentyEightDayWeightChangeAbstainsWhenEitherPeriodHasSparseData()
    {
        var logs = new[]
        {
            WeightLog(new DateOnly(2026, 10, 18), 70), WeightLog(new DateOnly(2026, 10, 20), 71),
            WeightLog(new DateOnly(2026, 10, 31), 72), WeightLog(new DateOnly(2026, 11, 1), 69),
            WeightLog(new DateOnly(2026, 11, 6), 70)
        };

        var result = WeeklyAnalytics.Summarize(new DateOnly(2026, 11, 14), [], logs, plannedSessionsInWindow: 0);

        Assert.Null(result.WeightChangeKg28Days);
        Assert.Contains(result.DataQualityWarnings, warning => warning.Contains("28 dias", StringComparison.OrdinalIgnoreCase));
    }

    private static DailyLog WeightLog(DateOnly date, decimal weight) =>
        new(Guid.NewGuid(), date, weightKg: new Quantity(weight, "kg", QuantitySource.Measured));
}
