using PersonalFitnessAgent.App;
using PersonalFitnessAgent.Core.Domain;
using PersonalFitnessAgent.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace PersonalFitnessAgent.Core.Tests.Application;

public sealed class FitnessDataServiceTests
{
    [Fact]
    public void ScheduleIsSavedLocallyAndRestoredByService()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using (var store = new SqliteLocalStore(path))
            {
                var service = new FitnessDataService(store);
                var saved = service.SaveSchedule([DayOfWeek.Monday, DayOfWeek.Friday]);

                Assert.Equal(1, saved.Version);
            }
            using (var store = new SqliteLocalStore(path))
            {
                var service = new FitnessDataService(store);

                Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Friday }, service.Schedule!.PlannedWeekdays.Order());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void ImportedWorkoutSheetPersistsLocallyAndReplacesPreviousImport()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        var importedAt = DateTimeOffset.UtcNow;
        var first = new WorkoutSheet(Guid.NewGuid(), importedAt,
        [
            new WorkoutSheetDay("A", "Plan A", "Synthetic summary",
                [new WorkoutSheetExercise(1, "Synthetic row", "3", "8-12", "60 s", "Keep torso steady.", "Main", "Record locally")],
                [])
        ]);
        try
        {
            WorkoutSheet saved;
            using (var store = new SqliteLocalStore(path))
            {
                var service = new FitnessDataService(store);
                saved = service.ImportWorkoutSheet(first);
            }
            using (var store = new SqliteLocalStore(path))
            {
                var service = new FitnessDataService(store);
                var restored = Assert.IsType<WorkoutSheet>(service.ImportedWorkoutSheet);
                Assert.Equal(saved.Id, restored.Id);
                Assert.Equal("Synthetic row", restored.Days.Single().Exercises.Single().Name);

                var replacement = first with { Id = Guid.NewGuid() };
                var replaced = service.ImportWorkoutSheet(replacement);
                Assert.Equal(saved.Id, replaced.Id);
                Assert.Single(service.ImportedWorkoutSheet!.Days);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void MeasurementCorrectionCanRestoreUnknownWithoutDeletingFoodsOrSymptoms()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var date = new DateOnly(2026, 11, 14);
            service.SaveCheckIn(date, 7, 80, 90, "arroz", "sensacao leve", "ombro", 3,
                "empurrar", symptomOnsetDate: date);

            var updated = service.UpdateDailyMeasurements(date, null, ActivityLevel.Unknown, null, null,
                QuantitySource.SelfReported);

            Assert.Null(updated.SleepHours);
            Assert.Null(updated.WeightKg);
            Assert.Null(updated.WaistCm);
            Assert.Equal(ActivityLevel.Unknown, (ActivityLevel)updated.Activity);
            Assert.Single(updated.Foods);
            Assert.Single(updated.Symptoms);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void FoodAndSymptomEntriesCanBeCorrectedAndDeletedByStableId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var date = new DateOnly(2026, 11, 14);
            var saved = service.SaveCheckIn(date, null, null, null, "banana", "sensacao leve", "ombro", 2,
                "empurrar", symptomOnsetDate: date);
            var foodId = saved.Foods.Single().Id;
            var symptom = saved.Symptoms.Single();

            var correctedFood = service.RenameFoodEntry(date, foodId, "banana madura");
            var correctedSymptom = service.UpdateSymptomEntry(date, symptom.Id, "sensacao moderada", "ombro",
                4, date, null, "empurrar");

            Assert.Equal("banana madura", correctedFood.Foods.Single().Name);
            Assert.Equal(symptom.Id, correctedSymptom.Symptoms.Single().Id);
            Assert.Equal(symptom.RecordedUtc, correctedSymptom.Symptoms.Single().RecordedUtc);
            Assert.Equal("sensacao moderada", correctedSymptom.Symptoms.Single().Description);
            Assert.Empty(service.DeleteFoodEntry(date, foodId).Foods);
            Assert.Empty(service.DeleteSymptomEntry(date, symptom.Id).Symptoms);

            var catalogFood = service.SaveCatalogFood(date, FoodCatalog.Items[0], 100).Foods.Single();
            var renamedCatalogFood = service.RenameFoodEntry(date, catalogFood.Id, "unverified food name").Foods.Single();
            Assert.Null(renamedCatalogFood.EnergyKcal);
            Assert.Null(renamedCatalogFood.CompositionSource);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void CatalogFoodPortionCanBeCorrectedWithoutChangingItsIdentity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var date = new DateOnly(2026, 11, 14);
            var original = service.SaveCatalogFood(date, FoodCatalog.Items[0], 100).Foods.Single();

            var corrected = service.UpdateCatalogFoodQuantity(date, original.Id, 50).Foods.Single();
            Assert.Equal(original.Id, corrected.Id);
            Assert.Equal(50m, corrected.Amount);
            Assert.Equal("50 g", corrected.Description);
            Assert.Equal(64m, corrected.EnergyKcal);
            Assert.Equal("TACO-0003", corrected.CatalogCode);

            var unknown = service.UpdateCatalogFoodQuantity(date, original.Id, null).Foods.Single();
            Assert.False(unknown.Quantified);
            Assert.Null(unknown.Amount);
            Assert.Null(unknown.EnergyKcal);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.UpdateCatalogFoodQuantity(date, original.Id, -1));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void LegacySymptomIdsAreStableWhenLoadedFromOlderContracts()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var logId = Guid.NewGuid();
            var legacy = new DailyLogDto(logId, new DateOnly(2026, 11, 14), null, 0, null, null,
                null, null, [], [new SymptomDto("region", null, "synthetic note", null, [],
                    DateTimeOffset.UtcNow, 0, null, null)]);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(legacy));
            store.Save("daily-log", logId, 2, document.RootElement, DateTimeOffset.UtcNow, 0, 0);
            var service = new FitnessDataService(store);

            var firstId = service.DailyLogs.Single().Symptoms.Single().Id;
            var secondId = service.DailyLogs.Single().Symptoms.Single().Id;

            Assert.NotEqual(Guid.Empty, firstId);
            Assert.Equal(firstId, secondId);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void ExerciseLoadMetadataFlowsIntoRestrictionEvaluationAndPlanSnapshots()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            service.AddRestriction("no external load", null, null, null, true, null,
                RestrictionAction.Block, RestrictionSource.UserReport, "synthetic test policy");
            var safe = service.SaveExercise("bodyweight movement", "push", "floor", usesExternalLoad: false,
                region: "upper", position: "lying", rangeOfMotion: "partial");
            var blocked = service.SaveExercise("loaded movement", "push", "bar", usesExternalLoad: true);
            var unknown = service.SaveExercise("unclassified movement", "", "");

            Assert.Equal(RestrictionDecision.NoPolicyMatched, service.EvaluatePlan([safe.Id]).Single().Evaluation.Decision);
            Assert.Equal(RestrictionDecision.Block, service.EvaluatePlan([blocked.Id]).Single().Evaluation.Decision);
            Assert.Equal(RestrictionDecision.RequiresReview, service.EvaluatePlan([unknown.Id]).Single().Evaluation.Decision);
            Assert.Throws<InvalidOperationException>(() => service.SavePlan("blocked", [blocked.Id]));
            Assert.Throws<InvalidOperationException>(() => service.SavePlan("unknown", [unknown.Id]));

            var plan = service.SavePlan("safe", [safe.Id]);
            var snapshot = plan.Exercises.Single();
            Assert.False(snapshot.UsesExternalLoad);
            Assert.Equal("upper", snapshot.Region);
            Assert.Equal("lying", snapshot.Position);
            Assert.Equal("partial", snapshot.RangeOfMotion);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void StartingSavedPlanRechecksRestrictionsAddedAfterPlanCreation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var exercise = service.SaveExercise("synthetic press", "press", "bar", usesExternalLoad: true);
            var plan = service.SavePlan("synthetic plan", [exercise.Id]);
            service.AddRestriction("new load restriction", null, null, null, true, null,
                RestrictionAction.Block, RestrictionSource.UserReport, "synthetic policy");

            Assert.Throws<InvalidOperationException>(() => service.StartSession(plan));
            Assert.Empty(service.Sessions);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void StartingPlanWithWarningRequiresExplicitAcknowledgment()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var exercise = service.SaveExercise("synthetic press", "press", "bar", usesExternalLoad: true);
            var plan = service.SavePlan("synthetic plan", [exercise.Id]);
            service.AddRestriction("press warning", "press", null, null, null, null,
                RestrictionAction.Warn, RestrictionSource.UserReport, "synthetic policy");

            Assert.Throws<InvalidOperationException>(() => service.StartSession(plan));
            var session = service.StartSession(plan, warningsAcknowledged: true);
            Assert.Equal(plan.Id, session.PlanId);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void OlderExerciseContractsKeepNewRestrictionMetadataUnknown()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var id = Guid.NewGuid();
            using var payload = JsonDocument.Parse($"{{\"id\":\"{id:D}\",\"version\":1,\"name\":\"legacy\",\"movementPatterns\":[\"push\"],\"equipment\":[\"bar\"]}}");
            store.Save("exercise", Guid.NewGuid(), 3, payload.RootElement, DateTimeOffset.UtcNow, 0, 0);
            var service = new FitnessDataService(store);
            service.AddRestriction("load needs review", null, null, null, true, null,
                RestrictionAction.Block, RestrictionSource.UserReport, "synthetic test policy");

            var exercise = Assert.Single(service.Exercises);

            Assert.Null(exercise.UsesExternalLoad);
            Assert.Equal(RestrictionDecision.RequiresReview, service.EvaluatePlan([exercise.Id]).Single().Evaluation.Decision);
            Assert.Throws<InvalidOperationException>(() => service.SavePlan("legacy", [exercise.Id]));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    [Fact]
    public void CsvExportIncludesWorkoutHistoryHealthLogsRestrictionsAndSchedules()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pfa-{Guid.NewGuid():N}.db");
        try
        {
            using var store = new SqliteLocalStore(path);
            var service = new FitnessDataService(store);
            var exercise = service.SaveExercise("synthetic row", "pull", "cable", usesExternalLoad: true,
                region: "upper", position: "seated", rangeOfMotion: "full");
            var plan = service.SavePlan("synthetic plan", [exercise.Id]);
            var session = service.StartSession(plan);
            var set = service.AddSet(session.Id, exercise.Id, 12.5m, 8, 2, "comma, and \"quote\"");
            service.CompleteSession(session.Id, 35, "synthetic notes");
            var date = new DateOnly(2026, 11, 14);
            service.SaveCheckIn(date, 7.5m, 70, 80, null, "synthetic delayed note", "upper", 2,
                "pull", symptomOnsetDate: date, relatedSessionId: session.Id);
            service.SaveCatalogFood(date, FoodCatalog.Items[0], 100);
            service.AddRestriction("synthetic restriction", "pull", null, null, true, null,
                RestrictionAction.Block, RestrictionSource.UserReport, "synthetic policy");
            service.SaveSchedule([DayOfWeek.Monday, DayOfWeek.Friday]);

            var csv = service.ExportCsv();
            var rows = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var expectedColumns = CountCsvColumns(rows[0]);

            Assert.All(rows, row => Assert.Equal(expectedColumns, CountCsvColumns(row)));
            Assert.Contains("\"exercise\"", csv);
            Assert.Contains("\"plan_exercise\"", csv);
            Assert.Contains("\"session\"", csv);
            Assert.Contains("\"set\"", csv);
            Assert.Contains("\"daily_log\"", csv);
            Assert.Contains("\"food\"", csv);
            Assert.Contains("\"symptom\"", csv);
            Assert.Contains("\"restriction\"", csv);
            Assert.Contains("\"schedule_day\"", csv);
            Assert.Contains("\"comma, and \"\"quote\"\"\"", csv);
            Assert.Contains(set.Sets.Single().Id.ToString("D"), csv);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + "-shm")) File.Delete(path + "-shm");
            if (File.Exists(path + "-wal")) File.Delete(path + "-wal");
        }
    }

    private static int CountCsvColumns(string line)
    {
        var columns = 1;
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"') index++;
                else quoted = !quoted;
            }
            else if (line[index] == ',' && !quoted) columns++;
        }
        Assert.False(quoted);
        return columns;
    }
}
