using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalFitnessAgent.Core.Application;
using PersonalFitnessAgent.Core.Domain;
using PersonalFitnessAgent.Core.Time;
using PersonalFitnessAgent.Infrastructure.Sqlite;

namespace PersonalFitnessAgent.App;

public sealed record ExerciseDto(Guid Id, int Version, string Name, string[] MovementPatterns, string[] Equipment,
    bool? UsesExternalLoad = null, string? Region = null, string? Position = null, string? RangeOfMotion = null);
public sealed record ExerciseSnapshotDto(Guid Id, int Version, string Name, string[] MovementPatterns, string[] Equipment,
    bool? UsesExternalLoad = null, string? Region = null, string? Position = null, string? RangeOfMotion = null);
public sealed record PlanDto(Guid Id, int Version, string Label, ExerciseSnapshotDto[] Exercises);
public sealed record SetDto(Guid Id, Guid ExerciseId, int ExerciseVersion, int Index, decimal? LoadKg, int? Reps, decimal? Rir, string? Notes);
public sealed record SessionDto(Guid Id, Guid PlanId, int PlanVersion, DateTimeOffset StartedUtc, int OffsetMinutes,
    DateTimeOffset? CompletedUtc, int? DurationMinutes, string? Notes, ExerciseSnapshotDto[] PlanSnapshot, SetDto[] Sets)
{
    [JsonIgnore]
    public DateTimeOffset StartedAtLocal => StartedUtc.ToOffset(TimeSpan.FromMinutes(OffsetMinutes));
}
public sealed record FoodDto(Guid Id, string Name, string Description, bool Quantified, decimal? Amount, string? Unit,
    decimal? EnergyKcal, string? CatalogCode, string? CompositionSource, int? CompositionSourceVersion,
    DateTimeOffset RecordedUtc, int OffsetMinutes);
public sealed record SymptomDto(string Region, int? Severity, string Description, Guid? RelatedSessionId,
    string[] MovementPatterns, DateTimeOffset RecordedUtc, int OffsetMinutes, DateTimeOffset? OnsetUtc,
    int? OnsetOffsetMinutes, Guid Id = default);
public sealed record DailyLogDto(Guid Id, DateOnly LocalDate, decimal? SleepHours, int Activity, decimal? WeightKg,
    int? WeightSource, decimal? WaistCm, int? WaistSource, FoodDto[] Foods, SymptomDto[] Symptoms);
public sealed record RestrictionDto(Guid Id, string Label, string? Region, string? MovementPattern, string? Equipment,
    bool? ExternalLoad, string? Position, string? RangeOfMotion, int Action, int Source, int Status, string Reason,
    int Version, DateOnly? ReviewOn);
public sealed record WorkoutScheduleDto(Guid Id, int Version, DayOfWeek[] PlannedWeekdays);

public sealed class FitnessDataService
{
    private const int ContractVersion = 4;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SqliteLocalStore _store;

    public FitnessDataService(SqliteLocalStore store) => _store = store;

    public IReadOnlyList<ExerciseDto> Exercises => Read<ExerciseDto>("exercise").GroupBy(e => e.Id)
        .Select(group => group.OrderByDescending(e => e.Version).First()).OrderBy(e => e.Name).ToArray();
    public IReadOnlyList<PlanDto> Plans => Read<PlanDto>("plan").GroupBy(p => p.Id)
        .Select(group => group.OrderByDescending(p => p.Version).First()).OrderBy(p => p.Label).ToArray();
    public IReadOnlyList<SessionDto> Sessions => Read<SessionDto>("session");
    public IReadOnlyList<DailyLogDto> DailyLogs => Read<DailyLogDto>("daily-log");
    public IReadOnlyList<RestrictionDto> Restrictions => Read<RestrictionDto>("restriction").GroupBy(policy => policy.Id)
        .Select(group => group.OrderByDescending(policy => policy.Version).First()).ToArray();
    public WorkoutScheduleDto? Schedule => Read<WorkoutScheduleDto>("schedule").OrderByDescending(schedule => schedule.Version).FirstOrDefault();
    public PlanDto? NextPlan
    {
        get
        {
            var plans = Plans;
            if (plans.Count == 0) return null;
            var domainPlans = plans.Select(plan => new WorkoutPlan(plan.Id, plan.Version, plan.Label,
                plan.Exercises.Select(exercise => new ExercisePrescription(exercise.Id, exercise.Version)))).ToArray();
            var next = PlanAlternation.Next(domainPlans, Sessions.Select(ToDomain).ToArray());
            return next is null ? null : plans.First(plan => plan.Id == next.Id);
        }
    }

    public ExerciseDto SaveExercise(string name, string patterns, string equipment, Guid? existingExerciseId = null,
        bool? usesExternalLoad = null, string? region = null, string? position = null, string? rangeOfMotion = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Informe o nome do exercício.", nameof(name));
        var previous = existingExerciseId is null
            ? Exercises.FirstOrDefault(e => e.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
            : Exercises.FirstOrDefault(e => e.Id == existingExerciseId.Value)
                ?? throw new KeyNotFoundException("Exercício selecionado não encontrado.");
        var dto = new ExerciseDto(previous?.Id ?? Guid.NewGuid(), (previous?.Version ?? 0) + 1, name.Trim(),
            Split(patterns), Split(equipment), usesExternalLoad, EmptyToNull(region), EmptyToNull(position), EmptyToNull(rangeOfMotion));
        Save("exercise", Guid.NewGuid(), dto, auditAction: "exercise.created_or_versioned", auditDetail: dto.Name);
        return dto;
    }

    public PlanDto SavePlan(string label, IEnumerable<Guid> exerciseIds)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Informe o nome do plano.", nameof(label));
        var snapshots = exerciseIds.Distinct().Select(id => Exercises.FirstOrDefault(exercise => exercise.Id == id)
            ?? throw new KeyNotFoundException("Um exercício selecionado não existe no catálogo."))
            .Select(exercise => new ExerciseSnapshotDto(exercise.Id, exercise.Version, exercise.Name,
                exercise.MovementPatterns.ToArray(), exercise.Equipment.ToArray(), exercise.UsesExternalLoad,
                exercise.Region, exercise.Position, exercise.RangeOfMotion)).ToArray();
        if (snapshots.Length == 0) throw new ArgumentException("Adicione pelo menos um exercício ao plano.", nameof(exerciseIds));
        foreach (var exercise in snapshots)
        {
            var review = RestrictionEngine.Evaluate(new ExerciseContext(exercise.Id, exercise.Name,
                MetadataSet(exercise.MovementPatterns), MetadataSet(exercise.Equipment), exercise.UsesExternalLoad,
                exercise.Position, exercise.RangeOfMotion, exercise.Region),
                Restrictions.Select(ToDomain), DateTimeOffset.UtcNow);
            if (review.Decision is RestrictionDecision.Block or RestrictionDecision.RequiresReview)
                throw new InvalidOperationException($"{exercise.Name}: política exige revisão antes de incluir este exercício no plano.");
        }
        var previous = Plans.FirstOrDefault(p => p.Label.Equals(label.Trim(), StringComparison.OrdinalIgnoreCase));
        var dto = new PlanDto(previous?.Id ?? Guid.NewGuid(), (previous?.Version ?? 0) + 1, label.Trim(), snapshots);
        Save("plan", Guid.NewGuid(), dto, auditAction: "plan.versioned", auditDetail: dto.Label);
        return dto;
    }

    public IReadOnlyList<(ExerciseDto Exercise, RestrictionEvaluation Evaluation)> EvaluatePlan(IEnumerable<Guid> exerciseIds)
    {
        var results = new List<(ExerciseDto, RestrictionEvaluation)>();
        foreach (var id in exerciseIds.Distinct())
        {
            var exercise = Exercises.FirstOrDefault(item => item.Id == id) ?? throw new KeyNotFoundException("Exercício selecionado não encontrado.");
            var evaluation = RestrictionEngine.Evaluate(new ExerciseContext(exercise.Id, exercise.Name,
                MetadataSet(exercise.MovementPatterns), MetadataSet(exercise.Equipment), exercise.UsesExternalLoad,
                exercise.Position, exercise.RangeOfMotion, exercise.Region),
                Restrictions.Select(ToDomain), DateTimeOffset.UtcNow);
            results.Add((exercise, evaluation));
        }
        return results;
    }

    public SessionDto StartSession(PlanDto plan)
    {
        var dto = new SessionDto(Guid.NewGuid(), plan.Id, plan.Version, DateTimeOffset.UtcNow,
            (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes, null, null, null, plan.Exercises, []);
        Save("session", dto.Id, dto, auditAction: "session.started", auditDetail: plan.Label);
        return dto;
    }

    public SessionDto AddSet(Guid sessionId, Guid exerciseId, decimal? loadKg, int? reps, decimal? rir, string? notes)
    {
        var stored = Find<SessionDto>("session", sessionId) ?? throw new KeyNotFoundException("Treino não encontrado.");
        var exercise = stored.Value.PlanSnapshot.FirstOrDefault(item => item.Id == exerciseId) ?? throw new InvalidOperationException("O exercício não pertence à versão do plano iniciada.");
        var next = stored.Value with { Sets = [.. stored.Value.Sets, new SetDto(Guid.NewGuid(), exerciseId, exercise.Version,
            stored.Value.Sets.Count(s => s.ExerciseId == exerciseId) + 1, loadKg, reps, rir, notes)] };
        Save("session", sessionId, next, stored.Revision, "session.set_recorded", exercise.Name);
        return next;
    }

    public SessionDto CompleteSession(Guid sessionId, int? durationMinutes, string? notes)
    {
        var stored = Find<SessionDto>("session", sessionId) ?? throw new KeyNotFoundException("Treino não encontrado.");
        if (stored.Value.CompletedUtc is not null) throw new InvalidOperationException("Treino já concluído.");
        if (durationMinutes < 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        var next = stored.Value with { CompletedUtc = DateTimeOffset.UtcNow, DurationMinutes = durationMinutes, Notes = notes };
        Save("session", sessionId, next, stored.Revision, "session.completed", notes ?? "completed");
        return next;
    }

    public DailyLogDto SaveCheckIn(DateOnly date, decimal? sleepHours, decimal? weightKg, decimal? waistCm,
        string? foodDescription, string? symptomDescription, string? symptomRegion, int? severity, string? movementPatterns,
        DateOnly? symptomOnsetDate = null, Guid? relatedSessionId = null, int? activity = null,
        QuantitySource? measurementSource = null)
    {
        if (sleepHours is < 0 or > 24) throw new ArgumentOutOfRangeException(nameof(sleepHours));
        if (weightKg < 0 || waistCm < 0) throw new ArgumentOutOfRangeException(nameof(weightKg));
        var previous = DailyLogs.FirstOrDefault(log => log.LocalDate == date);
        var id = previous?.Id ?? Guid.NewGuid();
        var foods = string.IsNullOrWhiteSpace(foodDescription) ? previous?.Foods ?? [] : [.. previous?.Foods ?? [],
            new FoodDto(Guid.NewGuid(), foodDescription.Trim(), "", false, null, null, null, null, null, null, DateTimeOffset.UtcNow,
                (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes)];
        var symptoms = previous?.Symptoms ?? [];
        if (!string.IsNullOrWhiteSpace(symptomDescription))
        {
            var onsetLocal = (symptomOnsetDate ?? date).ToDateTime(TimeOnly.MinValue);
            var onsetUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(onsetLocal, DateTimeKind.Unspecified), TimeZoneInfo.Local), TimeSpan.Zero);
            symptoms = [.. symptoms, new SymptomDto(string.IsNullOrWhiteSpace(symptomRegion) ? "não informado" : symptomRegion.Trim(), severity, symptomDescription.Trim(), relatedSessionId,
                Split(movementPatterns), DateTimeOffset.UtcNow, (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes,
                onsetUtc, (int)TimeZoneInfo.Local.GetUtcOffset(onsetLocal).TotalMinutes, Guid.NewGuid())];
        }
        if (activity is not null && !Enum.IsDefined(typeof(ActivityLevel), activity.Value)) throw new ArgumentOutOfRangeException(nameof(activity));
        if (measurementSource is not null && !Enum.IsDefined(typeof(QuantitySource), measurementSource.Value)) throw new ArgumentOutOfRangeException(nameof(measurementSource));
        var source = (int)(measurementSource ?? QuantitySource.SelfReported);
        var dto = new DailyLogDto(id, date, sleepHours ?? previous?.SleepHours, activity ?? previous?.Activity ?? (int)ActivityLevel.Unknown,
            weightKg ?? previous?.WeightKg, weightKg is null ? previous?.WeightSource ?? source : source,
            waistCm ?? previous?.WaistCm, waistCm is null ? previous?.WaistSource ?? source : source, foods, symptoms);
        Save("daily-log", id, dto, Find<DailyLogDto>("daily-log", id)?.Revision ?? 0,
            "daily-log.updated", date.ToString("yyyy-MM-dd"));
        return dto;
    }

    public DailyLogDto UpdateDailyMeasurements(DateOnly date, decimal? sleepHours, ActivityLevel activity,
        decimal? weightKg, decimal? waistCm, QuantitySource measurementSource)
    {
        if (sleepHours is < 0 or > 24) throw new ArgumentOutOfRangeException(nameof(sleepHours));
        if (weightKg < 0) throw new ArgumentOutOfRangeException(nameof(weightKg));
        if (waistCm < 0) throw new ArgumentOutOfRangeException(nameof(waistCm));
        if (!Enum.IsDefined(activity)) throw new ArgumentOutOfRangeException(nameof(activity));
        if (!Enum.IsDefined(measurementSource)) throw new ArgumentOutOfRangeException(nameof(measurementSource));
        var previous = DailyLogs.FirstOrDefault(log => log.LocalDate == date);
        var id = previous?.Id ?? Guid.NewGuid();
        var dto = previous is null
            ? new DailyLogDto(id, date, sleepHours, (int)activity, weightKg, (int)measurementSource,
                waistCm, (int)measurementSource, [], [])
            : previous with
            {
                SleepHours = sleepHours,
                Activity = (int)activity,
                WeightKg = weightKg,
                WeightSource = weightKg is null ? null : (int)measurementSource,
                WaistCm = waistCm,
                WaistSource = waistCm is null ? null : (int)measurementSource
            };
        Save("daily-log", id, dto, Find<DailyLogDto>("daily-log", id)?.Revision ?? 0,
            "daily-log.measurements_corrected", date.ToString("yyyy-MM-dd"));
        return dto;
    }

    public DailyLogDto SaveCatalogFood(DateOnly date, FoodCatalogItem item, decimal? grams)
    {
        if (grams < 0) throw new ArgumentOutOfRangeException(nameof(grams));
        var previous = DailyLogs.FirstOrDefault(log => log.LocalDate == date);
        var now = DateTimeOffset.UtcNow;
        var offset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes;
        var food = new FoodDto(Guid.NewGuid(), item.Name,
            grams is null ? "" : $"{grams.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} g",
            grams is not null, grams, grams is null ? null : "g",
            FoodCatalog.EstimateEnergy(item, grams)?.Value,
            item.Code, item.Source, item.SourceVersion, now, offset);
        var dto = previous is null
            ? new DailyLogDto(Guid.NewGuid(), date, null, (int)ActivityLevel.Unknown, null, (int)QuantitySource.SelfReported,
                null, (int)QuantitySource.SelfReported, [food], [])
            : previous with { Foods = [.. previous.Foods, food] };
        Save("daily-log", dto.Id, dto, Find<DailyLogDto>("daily-log", dto.Id)?.Revision ?? 0,
            "food.catalog_entry_added", item.Code);
        return dto;
    }

    public DailyLogDto RenameFoodEntry(DateOnly date, Guid foodId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Food name is required.", nameof(name));
        var log = DailyLogs.FirstOrDefault(item => item.LocalDate == date) ?? throw new KeyNotFoundException("Daily log was not found.");
        if (!log.Foods.Any(food => food.Id == foodId)) throw new KeyNotFoundException("Food entry was not found.");
        var updated = log with
        {
            Foods = log.Foods.Select(food =>
            {
                if (food.Id != foodId) return food;
                var renamed = food with { Name = name.Trim() };
                return food.CatalogCode is not null && !food.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? renamed with { EnergyKcal = null, CatalogCode = null, CompositionSource = null, CompositionSourceVersion = null }
                    : renamed;
            }).ToArray()
        };
        Save("daily-log", log.Id, updated, Find<DailyLogDto>("daily-log", log.Id)?.Revision ?? 0,
            "food.entry_renamed", foodId.ToString("D"));
        return updated;
    }

    public DailyLogDto DeleteFoodEntry(DateOnly date, Guid foodId)
    {
        var log = DailyLogs.FirstOrDefault(item => item.LocalDate == date) ?? throw new KeyNotFoundException("Daily log was not found.");
        if (!log.Foods.Any(food => food.Id == foodId)) throw new KeyNotFoundException("Food entry was not found.");
        var updated = log with { Foods = log.Foods.Where(food => food.Id != foodId).ToArray() };
        Save("daily-log", log.Id, updated, Find<DailyLogDto>("daily-log", log.Id)?.Revision ?? 0,
            "food.entry_deleted", foodId.ToString("D"));
        return updated;
    }

    public DailyLogDto UpdateSymptomEntry(DateOnly date, Guid symptomId, string description, string region,
        int? severity, DateOnly onsetDate, Guid? relatedSessionId, string? movementPatterns)
    {
        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(region))
            throw new ArgumentException("Symptom description and region are required.");
        if (severity is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(severity));
        var log = DailyLogs.FirstOrDefault(item => item.LocalDate == date) ?? throw new KeyNotFoundException("Daily log was not found.");
        var previous = log.Symptoms.FirstOrDefault(item => item.Id == symptomId);
        if (previous is null) throw new KeyNotFoundException("Symptom entry was not found.");
        var onsetLocal = onsetDate.ToDateTime(TimeOnly.MinValue);
        var onsetUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(onsetLocal, DateTimeKind.Unspecified), TimeZoneInfo.Local), TimeSpan.Zero);
        var corrected = previous with
        {
            Description = description.Trim(), Region = region.Trim(), Severity = severity,
            RelatedSessionId = relatedSessionId, MovementPatterns = Split(movementPatterns), OnsetUtc = onsetUtc,
            OnsetOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(onsetLocal).TotalMinutes
        };
        var updated = log with { Symptoms = log.Symptoms.Select(item => item.Id == symptomId ? corrected : item).ToArray() };
        Save("daily-log", log.Id, updated, Find<DailyLogDto>("daily-log", log.Id)?.Revision ?? 0,
            "symptom.entry_corrected", symptomId.ToString("D"));
        return updated;
    }

    public DailyLogDto DeleteSymptomEntry(DateOnly date, Guid symptomId)
    {
        var log = DailyLogs.FirstOrDefault(item => item.LocalDate == date) ?? throw new KeyNotFoundException("Daily log was not found.");
        if (!log.Symptoms.Any(item => item.Id == symptomId)) throw new KeyNotFoundException("Symptom entry was not found.");
        var updated = log with { Symptoms = log.Symptoms.Where(item => item.Id != symptomId).ToArray() };
        Save("daily-log", log.Id, updated, Find<DailyLogDto>("daily-log", log.Id)?.Revision ?? 0,
            "symptom.entry_deleted", symptomId.ToString("D"));
        return updated;
    }

    public RestrictionDto AddRestriction(string label, string? pattern, string? region, string? equipment,
        bool? externalLoad, string? position, RestrictionAction action, RestrictionSource source, string reason,
        Guid? existingPolicyId = null, RestrictionStatus status = RestrictionStatus.Active)
    {
        var previous = existingPolicyId is null ? null : Restrictions.FirstOrDefault(item => item.Id == existingPolicyId.Value)
            ?? throw new KeyNotFoundException("Política selecionada não encontrada.");
        if (previous is not null && previous.Source == (int)RestrictionSource.Clinician)
            throw new InvalidOperationException("Políticas registradas como clínicas são imutáveis neste fluxo. Adicione uma nova política ou solicite uma revisão clínica.");
        var policy = new RestrictionPolicy(previous?.Id ?? Guid.NewGuid(), label, action, source, reason,
            movementPattern: EmptyToNull(pattern), region: EmptyToNull(region), equipment: EmptyToNull(equipment),
            externalLoad: externalLoad, position: EmptyToNull(position), version: (previous?.Version ?? 0) + 1, status: status);
        var dto = new RestrictionDto(policy.Id, policy.Label, policy.Region, policy.MovementPattern, policy.Equipment,
            policy.ExternalLoad, policy.Position, policy.RangeOfMotion, (int)policy.Action, (int)policy.Source,
            (int)policy.Status, policy.Reason, policy.Version, policy.ReviewOn);
        Save("restriction", Guid.NewGuid(), dto, auditAction: previous is null ? "restriction.created" : "restriction.versioned",
            auditDetail: dto.Label);
        return dto;
    }

    public WeeklySummary WeeklyReview(DateOnly date)
    {
        var schedule = Schedule;
        return WeeklyAnalytics.Summarize(date, Sessions.Select(ToDomain), DailyLogs.Select(ToDomain),
            schedule is null ? null : new WorkoutSchedule(schedule.PlannedWeekdays));
    }

    public WorkoutScheduleDto SaveSchedule(IEnumerable<DayOfWeek> plannedWeekdays)
    {
        var schedule = new WorkoutSchedule(plannedWeekdays);
        var previous = Schedule;
        var dto = new WorkoutScheduleDto(previous?.Id ?? Guid.NewGuid(), (previous?.Version ?? 0) + 1,
            schedule.PlannedWeekdays.Order().ToArray());
        Save("schedule", Guid.NewGuid(), dto, auditAction: "schedule.versioned",
            auditDetail: string.Join(",", dto.PlannedWeekdays));
        return dto;
    }

    public string ExportJson() => _store.ExportBackup();
    public string ExportEncrypted(string passphrase) => BackupEncryption.Encrypt(_store.ExportBackup(), passphrase);
    public BackupPreview PreviewImport(string json) => _store.PreviewBackup(json);
    public void Restore(string json) => _store.RestoreBackup(json);
    public void DeleteAllData() => _store.DeleteAllRecords();

    public string ExportCsv()
    {
        static string Cell(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        var headers = new[] { "record_type", "id", "version", "date_utc", "completed_utc", "date_local", "offset_minutes", "plan_id", "plan_version", "label",
            "exercise_id", "exercise_version", "exercise_name", "movement_patterns", "equipment", "uses_external_load", "region", "position", "range_of_motion",
            "set_index", "load_kg", "repetitions", "rir", "duration_minutes", "session_status", "sleep_hours", "activity", "weight_kg", "weight_source",
            "waist_cm", "waist_source", "food_name", "quantified", "amount", "unit", "energy_kcal_estimated", "composition_source", "symptom_id",
            "symptom_onset_utc", "symptom_onset_offset_minutes", "symptom_region", "symptom_severity", "related_session_id", "restriction_id", "restriction_action", "restriction_source",
            "restriction_status", "review_on", "weekday", "reason", "notes" };
        var column = headers.Select((name, index) => (name, index)).ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
        var lines = new List<string> { string.Join(',', headers.Select(Cell)) };
        void Add(string type, params (string Name, string? Value)[] fields)
        {
            var row = new string?[headers.Length];
            row[column["record_type"]] = type;
            foreach (var (name, value) in fields) row[column[name]] = value;
            lines.Add(string.Join(',', row.Select(Cell)));
        }
        static string? Number(object? value) => value switch
        {
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => null
        };
        static string? Stamp(DateTimeOffset? value) => value?.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        foreach (var exercise in Read<ExerciseDto>("exercise").OrderBy(item => item.Name).ThenBy(item => item.Version))
            Add("exercise", ("exercise_id", exercise.Id.ToString("D")), ("exercise_version", Number(exercise.Version)), ("exercise_name", exercise.Name),
                ("movement_patterns", string.Join("|", exercise.MovementPatterns)), ("equipment", string.Join("|", exercise.Equipment)),
                ("uses_external_load", exercise.UsesExternalLoad?.ToString()), ("region", exercise.Region), ("position", exercise.Position),
                ("range_of_motion", exercise.RangeOfMotion));

        foreach (var plan in Read<PlanDto>("plan").OrderBy(item => item.Label).ThenBy(item => item.Version))
            foreach (var exercise in plan.Exercises)
                Add("plan_exercise", ("plan_id", plan.Id.ToString("D")), ("plan_version", Number(plan.Version)), ("label", plan.Label),
                    ("exercise_id", exercise.Id.ToString("D")), ("exercise_version", Number(exercise.Version)), ("exercise_name", exercise.Name),
                    ("movement_patterns", string.Join("|", exercise.MovementPatterns)), ("equipment", string.Join("|", exercise.Equipment)),
                    ("uses_external_load", exercise.UsesExternalLoad?.ToString()), ("region", exercise.Region), ("position", exercise.Position),
                    ("range_of_motion", exercise.RangeOfMotion));

        foreach (var session in Sessions.OrderBy(item => item.StartedUtc))
        {
            var sessionStatus = session.CompletedUtc is null ? "in_progress" : "completed";
            Add("session", ("id", session.Id.ToString("D")), ("date_utc", Stamp(session.StartedUtc)), ("date_local", session.StartedAtLocal.ToString("O")),
                ("completed_utc", Stamp(session.CompletedUtc)), ("offset_minutes", Number(session.OffsetMinutes)),
                ("plan_id", session.PlanId.ToString("D")), ("plan_version", Number(session.PlanVersion)),
                ("duration_minutes", Number(session.DurationMinutes)), ("session_status", sessionStatus), ("notes", session.Notes));
            foreach (var set in session.Sets)
            {
                var exercise = session.PlanSnapshot.FirstOrDefault(item => item.Id == set.ExerciseId && item.Version == set.ExerciseVersion);
                Add("set", ("id", set.Id.ToString("D")), ("date_utc", Stamp(session.StartedUtc)), ("date_local", session.StartedAtLocal.ToString("O")),
                    ("completed_utc", Stamp(session.CompletedUtc)), ("offset_minutes", Number(session.OffsetMinutes)),
                    ("plan_id", session.PlanId.ToString("D")), ("plan_version", Number(session.PlanVersion)),
                    ("exercise_id", set.ExerciseId.ToString("D")), ("exercise_version", Number(set.ExerciseVersion)), ("exercise_name", exercise?.Name),
                    ("set_index", Number(set.Index)), ("load_kg", Number(set.LoadKg)), ("repetitions", Number(set.Reps)), ("rir", Number(set.Rir)), ("notes", set.Notes));
            }
        }

        foreach (var log in DailyLogs.OrderBy(item => item.LocalDate))
        {
            Add("daily_log", ("id", log.Id.ToString("D")), ("date_local", log.LocalDate.ToString("yyyy-MM-dd")),
                ("sleep_hours", Number(log.SleepHours)), ("activity", ((ActivityLevel)log.Activity).ToString()),
                ("weight_kg", Number(log.WeightKg)), ("weight_source", log.WeightSource is null ? null : ((QuantitySource)log.WeightSource.Value).ToString()),
                ("waist_cm", Number(log.WaistCm)), ("waist_source", log.WaistSource is null ? null : ((QuantitySource)log.WaistSource.Value).ToString()));
            foreach (var food in log.Foods)
                Add("food", ("id", food.Id.ToString("D")), ("date_utc", Stamp(food.RecordedUtc)), ("offset_minutes", Number(food.OffsetMinutes)),
                    ("date_local", food.RecordedUtc.ToOffset(TimeSpan.FromMinutes(food.OffsetMinutes)).ToString("O")), ("food_name", food.Name),
                    ("quantified", food.Quantified.ToString()), ("amount", Number(food.Amount)), ("unit", food.Unit),
                    ("energy_kcal_estimated", Number(food.EnergyKcal)),
                    ("composition_source", food.CompositionSource is null ? null : $"{food.CompositionSource} v{food.CompositionSourceVersion}"), ("notes", food.Description));
            foreach (var symptom in log.Symptoms)
                Add("symptom", ("symptom_id", symptom.Id.ToString("D")), ("date_utc", Stamp(symptom.RecordedUtc)),
                    ("date_local", symptom.RecordedUtc.ToOffset(TimeSpan.FromMinutes(symptom.OffsetMinutes)).ToString("O")),
                    ("offset_minutes", Number(symptom.OffsetMinutes)), ("symptom_onset_utc", Stamp(symptom.OnsetUtc)),
                    ("symptom_onset_offset_minutes", Number(symptom.OnsetOffsetMinutes)),
                    ("symptom_region", symptom.Region), ("symptom_severity", Number(symptom.Severity)),
                    ("related_session_id", symptom.RelatedSessionId?.ToString("D")), ("movement_patterns", string.Join("|", symptom.MovementPatterns)),
                    ("notes", symptom.Description));
        }

        foreach (var policy in Read<RestrictionDto>("restriction").OrderBy(item => item.Label).ThenBy(item => item.Version))
            Add("restriction", ("restriction_id", policy.Id.ToString("D")), ("version", Number(policy.Version)), ("label", policy.Label),
                ("region", policy.Region), ("movement_patterns", policy.MovementPattern), ("equipment", policy.Equipment),
                ("uses_external_load", policy.ExternalLoad?.ToString()), ("position", policy.Position), ("range_of_motion", policy.RangeOfMotion),
                ("restriction_action", ((RestrictionAction)policy.Action).ToString()), ("restriction_source", ((RestrictionSource)policy.Source).ToString()),
                ("restriction_status", ((RestrictionStatus)policy.Status).ToString()), ("review_on", policy.ReviewOn?.ToString("yyyy-MM-dd")), ("reason", policy.Reason));

        foreach (var schedule in Read<WorkoutScheduleDto>("schedule").OrderBy(item => item.Version))
            foreach (var weekday in schedule.PlannedWeekdays)
                Add("schedule_day", ("id", schedule.Id.ToString("D")), ("version", Number(schedule.Version)), ("weekday", weekday.ToString()));
        return string.Join(Environment.NewLine, lines);
    }

    private void Save<T>(string kind, Guid id, T value, long revision = 0, string? auditAction = null, string? auditDetail = null)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
        using var detail = JsonDocument.Parse(JsonSerializer.Serialize(new { detail = auditDetail }, JsonOptions));
        var audit = auditAction is null ? null : new AuditEnvelope(Guid.NewGuid(), auditAction, "user", DateTimeOffset.UtcNow,
            (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes, [id], detail.RootElement);
        _store.Save(kind, id, ContractVersion, document.RootElement, DateTimeOffset.UtcNow,
            (int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes, revision, audit);
    }

    private IReadOnlyList<T> Read<T>(string kind) => _store.ReadAll(kind)
        .Select(record => NormalizeLegacyDailyLog(record.Payload.Deserialize<T>(JsonOptions)!))
        .Where(item => item is not null).ToArray();

    private static T NormalizeLegacyDailyLog<T>(T item)
    {
        if (item is not DailyLogDto log || !log.Symptoms.Any(symptom => symptom.Id == Guid.Empty)) return item;
        var symptoms = log.Symptoms.Select((symptom, index) => symptom.Id != Guid.Empty
            ? symptom : symptom with { Id = StableLegacySymptomId(log.Id, index) }).ToArray();
        return (T)(object)(log with { Symptoms = symptoms });
    }

    private static Guid StableLegacySymptomId(Guid logId, int index)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{logId:N}:symptom:{index}"));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private (T Value, long Revision)? Find<T>(string kind, Guid id)
    {
        var record = _store.ReadAll(kind).FirstOrDefault(item => item.Id == id);
        return record is null ? null : (record.Payload.Deserialize<T>(JsonOptions)!, record.Revision);
    }

    private static string[] Split(string? values) => (values ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IReadOnlySet<string>? MetadataSet(IEnumerable<string>? values)
    {
        var set = new HashSet<string>(values ?? [], StringComparer.OrdinalIgnoreCase);
        return set.Count == 0 ? null : set;
    }

    private static WorkoutSession ToDomain(SessionDto session) => new(session.Id, session.PlanId, session.PlanVersion,
        new RecordedTimestamp(session.StartedUtc, TimeSpan.FromMinutes(session.OffsetMinutes)), session.Sets.Select(s =>
            new ExerciseSet(s.Id, s.ExerciseId, s.ExerciseVersion, s.Index, s.LoadKg, s.Reps, s.Rir, s.Notes)),
        session.CompletedUtc is null ? WorkoutSessionStatus.InProgress : WorkoutSessionStatus.Completed,
        session.CompletedUtc is null ? null : new RecordedTimestamp(session.CompletedUtc.Value, TimeSpan.FromMinutes(session.OffsetMinutes)),
        session.DurationMinutes is null ? null : TimeSpan.FromMinutes(session.DurationMinutes.Value), session.Notes);

    private static DailyLog ToDomain(DailyLogDto log) => new(log.Id, log.LocalDate, log.SleepHours,
        (ActivityLevel)log.Activity, log.Foods.Select(food => new FoodEntry(food.Id == Guid.Empty ? Guid.NewGuid() : food.Id, food.Name,
            food.Description, food.Amount is null || food.Unit is null ? null : new Quantity(food.Amount.Value, food.Unit, QuantitySource.SelfReported),
            food.EnergyKcal is null ? null : new Quantity(food.EnergyKcal.Value, "kcal", QuantitySource.Estimated), food.Quantified)),
        log.WeightKg is null ? null : new Quantity(log.WeightKg.Value, "kg", log.WeightSource is null ? QuantitySource.SelfReported : (QuantitySource)log.WeightSource.Value),
        log.WaistCm is null ? null : new Quantity(log.WaistCm.Value, "cm", log.WaistSource is null ? QuantitySource.SelfReported : (QuantitySource)log.WaistSource.Value),
        log.Symptoms.Select(symptom => new SymptomEvent(symptom.Id == Guid.Empty ? Guid.NewGuid() : symptom.Id,
            new RecordedTimestamp(symptom.RecordedUtc, TimeSpan.FromMinutes(symptom.OffsetMinutes)), symptom.OnsetUtc,
            symptom.Region, symptom.Severity, symptom.Description, symptom.RelatedSessionId, symptom.MovementPatterns)));

    private static RestrictionPolicy ToDomain(RestrictionDto policy) => new(policy.Id, policy.Label,
        (RestrictionAction)policy.Action, (RestrictionSource)policy.Source, policy.Reason, (RestrictionStatus)policy.Status,
        policy.Region, policy.MovementPattern, policy.Equipment, policy.ExternalLoad, policy.Position,
        policy.RangeOfMotion, policy.Version, policy.ReviewOn);
}
