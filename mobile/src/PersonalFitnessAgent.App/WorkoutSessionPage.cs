using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Offline workout logger. Loads are entered by the user and never adjusted automatically.</summary>
public sealed class WorkoutSessionPage : ContentPage
{
    private readonly WorkoutSheetDay _day;
    private readonly PlanDto _plan;
    private readonly int _initialExerciseIndex;
    private readonly Picker _exercisePicker = new() { IsVisible = false };
    private readonly Entry _load = new()
    {
        Placeholder = "Carga",
        Keyboard = Keyboard.Numeric,
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#F3F6FA"),
        PlaceholderColor = Color.FromArgb("#8997A6"),
        BackgroundColor = Color.FromArgb("#1B2732"),
        HeightRequest = 58
    };
    private readonly Entry _reps = new()
    {
        Placeholder = "Reps",
        Keyboard = Keyboard.Numeric,
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#F3F6FA"),
        PlaceholderColor = Color.FromArgb("#8997A6"),
        BackgroundColor = Color.FromArgb("#1B2732"),
        HeightRequest = 58
    };
    private readonly Entry _restSeconds = new() { Text = "90", Keyboard = Keyboard.Numeric };
    private readonly Label _status = new() { FontSize = 13, TextColor = Color.FromArgb("#FF9A6B"), IsVisible = false };
    private readonly Label _exerciseProgress = new() { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF8A52"), CharacterSpacing = 1.2 };
    private readonly Label _exerciseName = new() { FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _seriesHeading = new() { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A7B2BF"), CharacterSpacing = 1 };
    private readonly VerticalStackLayout _setHistory = new() { Spacing = 5 };
    private readonly VerticalStackLayout _seriesForm = new() { Spacing = 12 };
    private readonly Border _seriesPanel;
    private readonly Border _restPanel;
    private readonly Border _completePanel;
    private readonly Label _restCountdown = new() { Text = "01:30", FontSize = 54, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _restNextSeries = new() { FontSize = 14, TextColor = Color.FromArgb("#A7B2BF"), HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _restButton = new() { Text = "Pular descanso", HeightRequest = 52, CornerRadius = 17, BackgroundColor = Color.FromArgb("#FF642D"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 15 };
    private readonly Button _saveButton = new() { Text = "Série concluída", FontAttributes = FontAttributes.Bold, FontSize = 15, HeightRequest = 54, CornerRadius = 17, BackgroundColor = Color.FromArgb("#FF642D"), TextColor = Colors.White };
    private readonly Button _pauseButton = SecondaryButton("Pausar treino");
    private readonly IDispatcherTimer _timer;
    private DateTimeOffset? _restDeadline;
    private SessionDto? _session;
    private string? _queuedLoad;
    private bool _isResting;
    private bool _allExercisesComplete;
    private bool _finishing;

    public WorkoutSessionPage(WorkoutSheet sheet, WorkoutSheetDay day, int initialExerciseIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(day);
        _day = day;
        _initialExerciseIndex = Math.Clamp(initialExerciseIndex, 0, Math.Max(0, day.Exercises.Count - 1));
        _plan = CreatePlanFor(sheet, day, App.Data.Exercises);
        Title = $"Treino {day.Key}";
        BackgroundColor = Color.FromArgb("#0B0F14");

        foreach (var exercise in day.Exercises) _exercisePicker.Items.Add(exercise.Name);
        _exercisePicker.SelectedIndexChanged += (_, _) => ApplyExerciseSelection();

        _saveButton.Clicked += (_, _) => SaveSet();
        _restButton.Clicked += (_, _) => EndRest();
        _pauseButton.Clicked += async (_, _) => await PauseAndExitAsync();

        var loadField = InputField("CARGA · KG", _load);
        var repsField = InputField("REPETIÇÕES", _reps);
        var inputs = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { loadField, repsField }
        };
        inputs.SetColumn(repsField, 1);

        _seriesForm.Children.Add(_seriesHeading);
        _seriesForm.Children.Add(inputs);
        _seriesForm.Children.Add(_saveButton);
        _seriesPanel = new Border
        {
            Stroke = Color.FromArgb("#344252"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(16),
            Content = _seriesForm
        };

        _restPanel = new Border
        {
            IsVisible = false,
            Stroke = Color.FromArgb("#344252"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(18),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "DESCANSO", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF8A52"), CharacterSpacing = 1.1, HorizontalTextAlignment = TextAlignment.Center },
                    _restNextSeries,
                    _restCountdown,
                    _restButton
                }
            }
        };

        var completeButton = new Button
        {
            Text = "Concluir treino",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            HeightRequest = 54,
            CornerRadius = 17,
            BackgroundColor = Color.FromArgb("#FF642D"),
            TextColor = Colors.White
        };
        completeButton.Clicked += async (_, _) => await FinishAsync();
        _completePanel = new Border
        {
            IsVisible = false,
            Stroke = Color.FromArgb("#344252"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(18),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Treino concluído", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), HorizontalTextAlignment = TextAlignment.Center },
                    completeButton
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16, 20, 28),
                Spacing = 14,
                Children =
                {
                    _exerciseProgress,
                    _exerciseName,
                    _setHistory,
                    _seriesPanel,
                    _restPanel,
                    _completePanel,
                    _status,
                    _pauseButton
                }
            }
        };

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => UpdateCountdown();
        if (_day.Exercises.Count > 0) _exercisePicker.SelectedIndex = _initialExerciseIndex;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshSessionUi();
    }

    protected override void OnDisappearing()
    {
        if (_session is { CompletedUtc: null, PausedUtc: null } && !_finishing)
            PauseCurrentSession();
        _timer.Stop();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = PauseAndExitAsync();
        return true;
    }

    public async Task<bool> StartAsync()
    {
        var pending = App.Data.Sessions
            .Where(session => session.PlanId == _plan.Id && session.CompletedUtc is null)
            .OrderByDescending(session => session.StartedUtc)
            .FirstOrDefault();
        var targetPlan = pending is null
            ? _plan
            : new PlanDto(pending.PlanId, pending.PlanVersion, _day.Title, pending.PlanSnapshot);

        var reviews = App.Data.EvaluatePlan(targetPlan);
        var blocked = reviews.Where(item => item.Evaluation.Decision is RestrictionDecision.Block or RestrictionDecision.RequiresReview).ToArray();
        if (blocked.Length > 0)
        {
            await DisplayAlertAsync("Revisão necessária",
                string.Join("\n", blocked.Select(item => item.Exercise.Name + ": é necessária uma revisão antes de iniciar.")), "OK");
            return false;
        }

        var warnings = reviews.Where(item => item.Evaluation.Decision == RestrictionDecision.Warn).ToArray();
        if (warnings.Length > 0)
        {
            var acknowledged = await DisplayAlertAsync("Aviso",
                string.Join("\n", warnings.Select(item => item.Exercise.Name + ": há um aviso registrado.")),
                "Continuar", "Voltar");
            if (!acknowledged) return false;
        }

        try
        {
            if (pending is not null)
            {
                _session = App.Data.ResumeSession(pending.Id);
                var nextExercise = FindNextExerciseIndex(_session);
                _allExercisesComplete = IsWorkoutComplete(_session);
                _exercisePicker.SelectedIndex = nextExercise;
                _status.Text = string.Empty;
                PrefillLoadFromLastSet();
            }
            else
            {
                _session = App.Data.StartSession(_plan, warningsAcknowledged: warnings.Length > 0);
                _exercisePicker.SelectedIndex = _initialExerciseIndex;
            }

            RefreshSessionUi();
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or ArgumentOutOfRangeException)
        {
            await DisplayAlertAsync("Não foi possível abrir o treino", exception.Message, "OK");
            return false;
        }
    }

    private void ApplyExerciseSelection()
    {
        if (_exercisePicker.SelectedIndex < 0) return;
        var index = _exercisePicker.SelectedIndex;
        _restSeconds.Text = ParseRestSeconds(_day.Exercises[index].Rest).ToString(CultureInfo.InvariantCulture);
        if (_session is { CompletedUtc: null, PausedUtc: null } && _session.ActiveExerciseIndex != index)
        {
            try { _session = App.Data.UpdateSessionProgress(_session.Id, index); }
            catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or ArgumentOutOfRangeException)
            {
                ShowStatus($"Não foi possível abrir o exercício: {exception.Message}");
            }
        }
        RefreshSessionUi();
    }

    private void RefreshSessionUi()
    {
        if (_exercisePicker.SelectedIndex < 0 || _day.Exercises.Count == 0) return;
        var index = _exercisePicker.SelectedIndex;
        var exercise = _plan.Exercises[index];
        var savedSets = _session?.Sets.Where(set => set.ExerciseId == exercise.Id).OrderBy(set => set.Index).ToArray() ?? [];
        var plannedSets = ParsePlannedSets(_day.Exercises[index].Sets);
        _exerciseProgress.Text = $"EXERCÍCIO {index + 1} / {_day.Exercises.Count}";
        _seriesHeading.Text = plannedSets is null
            ? $"SÉRIE {savedSets.Length + 1}"
            : $"SÉRIE {savedSets.Length + 1} DE {plannedSets}";
        _exerciseName.Text = exercise.Name;

        _setHistory.Children.Clear();
        foreach (var set in savedSets) _setHistory.Children.Add(CreateSetRow(set));
        _setHistory.IsVisible = savedSets.Length > 0;

        var active = _session is { CompletedUtc: null, PausedUtc: null };
        var showForm = active && !_isResting && !_allExercisesComplete;
        _seriesPanel.IsVisible = showForm;
        _restPanel.IsVisible = active && _isResting;
        _completePanel.IsVisible = active && !_isResting && _allExercisesComplete;
        _pauseButton.IsVisible = active;
        _restButton.IsEnabled = active && _isResting;
        _saveButton.IsEnabled = showForm && !_finishing;
        _exercisePicker.IsEnabled = active && !_isResting;
        _status.IsVisible = !string.IsNullOrWhiteSpace(_status.Text);
    }

    private static View InputField(string title, Entry entry) => new VerticalStackLayout
    {
        Spacing = 5,
        Children =
        {
            new Label { Text = title, FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A7B2BF"), CharacterSpacing = 0.7 },
            new Border
            {
                Stroke = Color.FromArgb("#344252"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                BackgroundColor = Color.FromArgb("#1B2732"),
                Padding = new Thickness(10, 0),
                Content = entry
            }
        }
    };

    private static View CreateSetRow(SetDto set)
    {
        var load = set.LoadKg is null ? "—" : $"{set.LoadKg.Value.ToString("0.##", CultureInfo.CurrentCulture)} kg";
        var reps = set.Reps is null ? "—" : $"{set.Reps} reps";
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        row.Add(new Label { Text = $"✓  Série {set.Index}", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF8A52"), VerticalTextAlignment = TextAlignment.Center });
        row.Add(new Label { Text = $"{load}   ×   {reps}", FontSize = 13, TextColor = Color.FromArgb("#F3F6FA"), VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.End }, 1, 0);
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            BackgroundColor = Color.FromArgb("#1B2732"),
            Padding = new Thickness(12, 8),
            Content = row
        };
    }

    private void SaveSet()
    {
        if (_session is null || _exercisePicker.SelectedIndex < 0) return;

        decimal? load = null;
        if (!string.IsNullOrWhiteSpace(_load.Text))
        {
            if (!decimal.TryParse(_load.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedLoad) || parsedLoad < 0)
            {
                ShowStatus("Confira a carga.");
                return;
            }
            load = parsedLoad;
        }

        int? reps = null;
        if (!string.IsNullOrWhiteSpace(_reps.Text))
        {
            if (!int.TryParse(_reps.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedReps) || parsedReps < 0)
            {
                ShowStatus("Confira as repetições.");
                return;
            }
            reps = parsedReps;
        }

        if (load is null && reps is null)
        {
            ShowStatus("Informe carga ou repetições.");
            return;
        }

        try
        {
            var exercise = _plan.Exercises[_exercisePicker.SelectedIndex];
            _session = App.Data.AddSet(_session.Id, exercise.Id, load, reps, null, null);
            _queuedLoad = load?.ToString("0.##", CultureInfo.CurrentCulture);
            _load.Text = _reps.Text = string.Empty;
            _status.Text = string.Empty;
            StartRest();
            RefreshSessionUi();
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            ShowStatus($"Não foi possível salvar: {exception.Message}");
        }
    }

    private void StartRest()
    {
        if (!int.TryParse(_restSeconds.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds is < 1 or > 7200)
            seconds = 90;

        var index = _exercisePicker.SelectedIndex;
        var exercise = _plan.Exercises[index];
        var completedSets = _session?.Sets.Count(set => set.ExerciseId == exercise.Id) ?? 0;
        var plannedSets = ParsePlannedSets(_day.Exercises[index].Sets);
        var lastSet = plannedSets is not null && completedSets >= plannedSets;
        _restNextSeries.Text = lastSet
            ? index + 1 < _day.Exercises.Count ? $"Próximo: {_plan.Exercises[index + 1].Name}" : "Último exercício"
            : $"Série {completedSets + 1}";
        _restDeadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
        _isResting = true;
        UpdateCountdown();
        _timer.Start();
    }

    private void EndRest()
    {
        _timer.Stop();
        _restDeadline = null;
        _isResting = false;
        _restCountdown.Text = "00:00";

        var index = _exercisePicker.SelectedIndex;
        var exercise = _plan.Exercises[index];
        var completedSets = _session?.Sets.Count(set => set.ExerciseId == exercise.Id) ?? 0;
        var plannedSets = ParsePlannedSets(_day.Exercises[index].Sets);
        if (plannedSets is not null && completedSets >= plannedSets)
        {
            if (index + 1 < _day.Exercises.Count)
            {
                _load.Text = _reps.Text = string.Empty;
                _exercisePicker.SelectedIndex = index + 1;
                PrefillLoadFromLastSet();
            }
            else
            {
                _allExercisesComplete = true;
            }
        }
        else
        {
            _load.Text = _queuedLoad;
            PrefillLoadFromLastSet();
        }

        _queuedLoad = null;
        _status.Text = string.Empty;
        RefreshSessionUi();
    }

    private int FindNextExerciseIndex(SessionDto session)
    {
        for (var index = 0; index < _day.Exercises.Count; index++)
        {
            var exerciseId = _plan.Exercises[index].Id;
            var completedSets = session.Sets.Count(set => set.ExerciseId == exerciseId);
            var plannedSets = ParsePlannedSets(_day.Exercises[index].Sets);
            if (plannedSets is null ? completedSets == 0 : completedSets < plannedSets)
                return index;
        }

        return Math.Clamp(session.ActiveExerciseIndex, 0, Math.Max(0, _day.Exercises.Count - 1));
    }

    private bool IsWorkoutComplete(SessionDto session)
    {
        if (_day.Exercises.Count == 0) return false;
        for (var index = 0; index < _day.Exercises.Count; index++)
        {
            var plannedSets = ParsePlannedSets(_day.Exercises[index].Sets);
            if (plannedSets is null) return false;
            var exerciseId = _plan.Exercises[index].Id;
            if (session.Sets.Count(set => set.ExerciseId == exerciseId) < plannedSets)
                return false;
        }
        return true;
    }

    private void PrefillLoadFromLastSet()
    {
        if (_session is null || _exercisePicker.SelectedIndex < 0 || !string.IsNullOrWhiteSpace(_load.Text)) return;
        var exerciseId = _plan.Exercises[_exercisePicker.SelectedIndex].Id;
        var lastLoad = _session.Sets
            .Where(set => set.ExerciseId == exerciseId)
            .OrderBy(set => set.Index)
            .LastOrDefault()?.LoadKg;
        if (lastLoad is not null)
            _load.Text = lastLoad.Value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private void PauseCurrentSession()
    {
        if (_session is not { CompletedUtc: null, PausedUtc: null }) return;
        try
        {
            _session = App.Data.PauseSession(_session.Id);
            if (_isResting) PrefillLoadFromLastSet();
            _timer.Stop();
            _restDeadline = null;
            _isResting = false;
            _restCountdown.Text = "00:00";
            RefreshSessionUi();
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            ShowStatus($"Não foi possível pausar: {exception.Message}");
        }
    }

    private async Task PauseAndExitAsync()
    {
        if (_session is null) return;
        if (!string.IsNullOrWhiteSpace(_load.Text) || !string.IsNullOrWhiteSpace(_reps.Text))
        {
            var discard = await DisplayAlertAsync("Sair do treino?", "Os valores desta série não salvos serão descartados.", "Sair", "Continuar");
            if (!discard) return;
        }
        PauseCurrentSession();
        await Shell.Current.Navigation.PopAsync();
    }

    private async Task FinishAsync()
    {
        if (_session is null) return;
        if (_session.PausedUtc is not null)
        {
            await DisplayAlertAsync("Treino pausado", "Retome para concluí-lo.", "OK");
            return;
        }

        var elapsedSeconds = Math.Max(0, (DateTimeOffset.UtcNow - _session.StartedUtc).TotalSeconds - _session.TotalPausedSeconds);
        var minutes = Math.Max(0, (int)Math.Round(elapsedSeconds / 60));
        try
        {
            _finishing = true;
            _session = App.Data.CompleteSession(_session.Id, minutes, null);
            _timer.Stop();
            await DisplayAlertAsync("Treino salvo", $"{_session.Sets.Length} séries · {minutes} min", "OK");
            await Shell.Current.Navigation.PopAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            _finishing = false;
            ShowStatus($"Não foi possível concluir: {exception.Message}");
        }
    }

    private void UpdateCountdown()
    {
        if (_restDeadline is null) return;
        var remaining = Math.Max(0, (int)Math.Ceiling((_restDeadline.Value - DateTimeOffset.UtcNow).TotalSeconds));
        _restCountdown.Text = $"{remaining / 60:00}:{remaining % 60:00}";
        if (remaining == 0)
        {
            EndRest();
        }
    }

    private void ShowStatus(string message)
    {
        _status.Text = message;
        _status.IsVisible = true;
    }

    private static Button SecondaryButton(string text) => new()
    {
        Text = text,
        FontSize = 13,
        HeightRequest = 46,
        CornerRadius = 14,
        BackgroundColor = Color.FromArgb("#1B2732"),
        TextColor = Color.FromArgb("#F3F6FA"),
        Padding = new Thickness(8, 0)
    };

    private static int? ParsePlannedSets(string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(value ?? "", @"\d+");
        return match.Success && int.TryParse(match.Value, CultureInfo.InvariantCulture, out var count) && count > 0 ? count : null;
    }

    private static int ParseRestSeconds(string rest)
    {
        if (string.IsNullOrWhiteSpace(rest)) return 90;
        var matches = System.Text.RegularExpressions.Regex.Matches(rest, @"\d+");
        if (matches.Count == 0) return 90;
        var value = int.Parse(matches[^1].Value, CultureInfo.InvariantCulture);
        if (rest.Contains("min", StringComparison.OrdinalIgnoreCase)) value *= 60;
        return Math.Clamp(value, 1, 7200);
    }

    internal static PlanDto CreatePlanFor(WorkoutSheet sheet, WorkoutSheetDay day, IReadOnlyList<ExerciseDto> catalog)
    {
        var snapshots = day.Exercises.Select(item =>
        {
            var existing = catalog.FirstOrDefault(exercise => exercise.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
            return existing is null
                ? new ExerciseSnapshotDto(StableId($"{sheet.Id:N}:{day.Key}:{item.Order}:{item.Name}"), 1, item.Name, [], [])
                : new ExerciseSnapshotDto(existing.Id, existing.Version, existing.Name, existing.MovementPatterns, existing.Equipment,
                    existing.UsesExternalLoad, existing.Region, existing.Position, existing.RangeOfMotion);
        }).ToArray();
        return new PlanDto(StableId($"{sheet.Id:N}:{day.Key}:plan"), 1, day.Title, snapshots);
    }

    private static Guid StableId(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
