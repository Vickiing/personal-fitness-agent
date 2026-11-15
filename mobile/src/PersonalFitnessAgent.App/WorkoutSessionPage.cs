using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Offline session logger for an imported workout day. Loads are user entered and never auto-adjusted.</summary>
public sealed class WorkoutSessionPage : ContentPage
{
    private readonly WorkoutSheetDay _day;
    private readonly PlanDto _plan;
    private readonly int _initialExerciseIndex;
    private readonly Picker _exercisePicker = new() { Title = "Selecione um exercício" };
    private readonly Entry _load = new() { Placeholder = "Carga (kg)", Keyboard = Keyboard.Numeric };
    private readonly Entry _reps = new() { Placeholder = "Repetições", Keyboard = Keyboard.Numeric };
    private readonly Entry _rir = new() { Placeholder = "RIR (opcional)", Keyboard = Keyboard.Numeric };
    private readonly Entry _notes = new() { Placeholder = "Observação (opcional)" };
    private readonly Entry _restSeconds = new() { Text = "90", Keyboard = Keyboard.Numeric, WidthRequest = 90 };
    private readonly Label _status = new() { FontSize = 13, TextColor = Color.FromArgb("#A7B2BF") };
    private readonly Label _exerciseProgress = new() { FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), HorizontalTextAlignment = TextAlignment.Center };
    private readonly VerticalStackLayout _setHistory = new() { Spacing = 6 };
    private readonly VerticalStackLayout _seriesForm = new() { Spacing = 11 };
    private readonly Border _restPanel;
    private readonly Label _restHeading = new() { FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF8A52") };
    private readonly Label _restNextSeries = new() { FontSize = 14, TextColor = Color.FromArgb("#CFD8E2"), HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _restCountdown = new() { Text = "01:30", FontSize = 38, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _restButton = new() { Text = "Encerrar descanso", HeightRequest = 48, CornerRadius = 16, BackgroundColor = Color.FromArgb("#FF642D"), TextColor = Colors.White, FontAttributes = FontAttributes.Bold };
    private readonly Button _saveButton = new() { Text = "Salvar série", FontAttributes = FontAttributes.Bold, HeightRequest = 52, CornerRadius = 16, BackgroundColor = Color.FromArgb("#18212B"), TextColor = Colors.White };
    private readonly Button _resumeButton = SecondaryButton("Retomar sessão");
    private readonly IDispatcherTimer _timer;
    private DateTimeOffset? _restDeadline;
    private SessionDto? _session;
    private Guid? _editingSetId;
    private string? _queuedLoad;
    private bool _isResting;
    private bool _finishing;

    public WorkoutSessionPage(WorkoutSheet sheet, WorkoutSheetDay day, int initialExerciseIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(day);
        _day = day;
        _initialExerciseIndex = Math.Clamp(initialExerciseIndex, 0, day.Exercises.Count - 1);
        _plan = CreatePlanFor(sheet, day, App.Data.Exercises);
        Title = "Registrar treino";
        BackgroundColor = Color.FromArgb("#0B0F14");
        foreach (var exercise in day.Exercises) _exercisePicker.Items.Add(exercise.Name);
        _exercisePicker.SelectedIndex = 0;
        _exercisePicker.SelectedIndexChanged += (_, _) => ApplyExerciseSelection();

        _saveButton.Clicked += (_, _) => SaveSet();
        _restButton.Clicked += (_, _) => EndRest();
        var previous = SecondaryButton("‹ Exercício anterior");
        var next = SecondaryButton("Próximo exercício ›");
        previous.Clicked += (_, _) => SelectExercise(-1);
        next.Clicked += (_, _) => SelectExercise(1);
        var exerciseNavigation = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8,
            Children = { previous, next }
        };
        exerciseNavigation.SetColumn(next, 1);

        var pause = SecondaryButton("Pausar e sair");
        pause.Clicked += async (_, _) => await PauseAndExitAsync();
        _resumeButton.IsVisible = false;
        _resumeButton.Clicked += async (_, _) => await ResumeCurrentSessionAsync();

        _seriesForm.Children.Add(_load);
        _seriesForm.Children.Add(_reps);
        _seriesForm.Children.Add(_rir);
        _seriesForm.Children.Add(_notes);
        _seriesForm.Children.Add(_saveButton);
        _restPanel = new Border
        {
            IsVisible = false,
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    _restHeading,
                    _restNextSeries,
                    _restCountdown,
                    new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center, Children = { new Label { Text = "Descanso (s)", VerticalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("#A7B2BF") }, _restSeconds } },
                    _restButton
                }
            }
        };
        var finish = SecondaryButton("Concluir treino");
        finish.Clicked += async (_, _) => await FinishAsync();

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16, 20, 26),
            Spacing = 11,
            Children =
            {
                new Label { Text = _day.Title, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA") },
                new Label { Text = "Registre cada série. A carga é informada por você e fica salva neste aparelho.", FontSize = 13, TextColor = Color.FromArgb("#A7B2BF"), LineBreakMode = LineBreakMode.WordWrap },
                _exerciseProgress,
                exerciseNavigation,
                _exercisePicker,
                new Label { Text = "SÉRIES DESTE EXERCÍCIO", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A7B2BF"), CharacterSpacing = 1.1 },
                _setHistory,
                _seriesForm,
                _restPanel,
                _status,
                _resumeButton,
                pause,
                finish
            }
        };
        Content = new ScrollView { Content = content };
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => UpdateCountdown();
        ApplyExerciseSelection();
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
        var blocked = reviews.Where(x => x.Evaluation.Decision is RestrictionDecision.Block or RestrictionDecision.RequiresReview).ToArray();
        if (blocked.Length > 0)
        {
            await DisplayAlertAsync("Revisão necessária", string.Join("\n", blocked.Select(x => x.Exercise.Name + ": é necessária uma revisão antes de iniciar.")), "Entendi");
            return false;
        }

        var warnings = reviews.Where(x => x.Evaluation.Decision == RestrictionDecision.Warn).ToArray();
        var acknowledged = warnings.Length == 0;
        if (!acknowledged)
        {
            acknowledged = await DisplayAlertAsync("Aviso das suas restrições",
                string.Join("\n", warnings.Select(x => x.Exercise.Name + ": há um aviso registrado. Confirme que deseja prosseguir.")),
                "Confirmar aviso", "Voltar");
            if (!acknowledged) return false;
        }

        try
        {
            if (pending is not null)
            {
                var resume = await DisplayAlertAsync("Sessão pausada",
                    $"Há uma sessão não concluída de {pending.PlanSnapshot.Length} exercícios com {pending.Sets.Length} série(s) salva(s). Deseja retomá-la?",
                    "Retomar", "Agora não");
                if (!resume) return false;
                _session = App.Data.ResumeSession(pending.Id);
                _exercisePicker.SelectedIndex = Math.Clamp(_session.ActiveExerciseIndex, 0, _day.Exercises.Count - 1);
                PrefillLoadFromLastSet();
                _status.Text = "Sessão retomada. Suas séries salvas foram recuperadas.";
            }
            else
            {
                _session = App.Data.StartSession(_plan, acknowledged);
                if (_initialExerciseIndex > 0)
                    _session = App.Data.UpdateSessionProgress(_session.Id, _initialExerciseIndex);
                _exercisePicker.SelectedIndex = _initialExerciseIndex;
                _status.Text = "Sessão iniciada. O progresso será salvo neste aparelho.";
            }
            RefreshSessionUi();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Não foi possível iniciar", ex.Message, "OK");
            return false;
        }
    }

    private void SelectExercise(int offset)
    {
        if (_day.Exercises.Count == 0) return;
        var current = Math.Max(0, _exercisePicker.SelectedIndex);
        _exercisePicker.SelectedIndex = (current + offset + _day.Exercises.Count) % _day.Exercises.Count;
    }

    private void ApplyExerciseSelection()
    {
        if (_exercisePicker.SelectedIndex < 0) return;
        if (_session is { CompletedUtc: null, PausedUtc: null } && _session.ActiveExerciseIndex != _exercisePicker.SelectedIndex)
        {
            try { _session = App.Data.UpdateSessionProgress(_session.Id, _exercisePicker.SelectedIndex); }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentOutOfRangeException)
            {
                _status.Text = $"Não foi possível salvar o exercício atual: {ex.Message}";
            }
        }
        var prescribed = _day.Exercises[_exercisePicker.SelectedIndex].Rest;
        _restSeconds.Text = ParseRestSeconds(prescribed).ToString(CultureInfo.InvariantCulture);
        RefreshSessionUi();
        if (_session is null)
            _status.Text = string.IsNullOrWhiteSpace(prescribed)
                ? "Descanso sem prescrição; ajuste o cronômetro se desejar."
                : $"Descanso da ficha: {prescribed}. Você pode ajustar o cronômetro.";
    }

    private void RefreshSessionUi()
    {
        if (_exercisePicker.SelectedIndex < 0) return;
        var index = _exercisePicker.SelectedIndex;
        var exercise = _plan.Exercises[index];
        var sets = _session?.Sets.Where(set => set.ExerciseId == exercise.Id).OrderBy(set => set.Index).ToArray() ?? [];
        var planned = ParsePlannedSets(_day.Exercises[index].Sets);
        _exerciseProgress.Text = planned is null
            ? $"Exercício {index + 1} de {_day.Exercises.Count}  ·  {sets.Length} série(s)"
            : $"Exercício {index + 1} de {_day.Exercises.Count}  ·  {sets.Length}/{planned} séries";
        _setHistory.Children.Clear();
        if (sets.Length == 0)
        {
            _setHistory.Children.Add(new Label { Text = "Nenhuma série registrada.", FontSize = 13, TextColor = Color.FromArgb("#A7B2BF") });
        }
        else
        {
            foreach (var set in sets) _setHistory.Children.Add(CreateSetRow(set));
        }
        var canEdit = _session is { CompletedUtc: null, PausedUtc: null };
        _saveButton.IsEnabled = canEdit && !_finishing && !_isResting;
        _seriesForm.IsVisible = canEdit && !_isResting;
        _restPanel.IsVisible = canEdit && _isResting;
        _restButton.IsEnabled = canEdit && _isResting;
        _exercisePicker.IsEnabled = canEdit && !_isResting;
        _resumeButton.IsVisible = _session is { CompletedUtc: null, PausedUtc: not null };
    }

    private View CreateSetRow(SetDto set)
    {
        var load = set.LoadKg is null ? "sem carga" : $"{set.LoadKg.Value.ToString("0.##", CultureInfo.CurrentCulture)} kg";
        var reps = set.Reps is null ? "reps —" : $"{set.Reps} reps";
        var details = new Label
        {
            Text = $"Série {set.Index}: {load} · {reps}" + (set.Rir is null ? "" : $" · RIR {set.Rir.Value.ToString("0.#", CultureInfo.CurrentCulture)}"),
            FontSize = 13,
            TextColor = Color.FromArgb("#CFD8E2"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        var edit = new Button { Text = "Editar", FontSize = 12, HeightRequest = 40, Padding = new Thickness(10, 0), BackgroundColor = Color.FromArgb("#1B2732"), TextColor = Color.FromArgb("#F3F6FA"), CornerRadius = 12 };
        edit.Clicked += (_, _) => LoadSetForCorrection(set);
        var remove = new Button { Text = "Remover", FontSize = 12, HeightRequest = 40, Padding = new Thickness(10, 0), BackgroundColor = Color.FromArgb("#35241D"), TextColor = Color.FromArgb("#FF9A6B"), CornerRadius = 12 };
        remove.Clicked += async (_, _) => await DeleteSetAsync(set);
        var actions = new HorizontalStackLayout { Spacing = 4, Children = { edit, remove } };
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8,
            Children = { details, actions }
        };
        row.SetColumn(actions, 1);
        return new Border
        {
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(10, 6),
            Content = row
        };
    }

    private void LoadSetForCorrection(SetDto set)
    {
        _editingSetId = set.Id;
        _load.Text = set.LoadKg?.ToString(CultureInfo.CurrentCulture);
        _reps.Text = set.Reps?.ToString(CultureInfo.CurrentCulture);
        _rir.Text = set.Rir?.ToString(CultureInfo.CurrentCulture);
        _notes.Text = set.Notes;
        _saveButton.Text = $"Salvar correção da série {set.Index}";
        _status.Text = "Editando uma série salva. Confirme para substituir os valores.";
    }

    private async Task DeleteSetAsync(SetDto set)
    {
        var approved = await DisplayAlertAsync("Remover série?", $"A série {set.Index} será removida do registro local.", "Remover", "Cancelar");
        if (!approved || _session is null) return;
        try
        {
            _session = App.Data.DeleteSet(_session.Id, set.Id);
            ClearCorrection();
            RefreshSessionUi();
            _status.Text = "Série removida.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            _status.Text = $"Não foi possível remover: {ex.Message}";
        }
    }

    private void SaveSet()
    {
        if (_session is null || _exercisePicker.SelectedIndex < 0) return;
        decimal? load = null;
        if (!string.IsNullOrWhiteSpace(_load.Text))
        {
            if (!decimal.TryParse(_load.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedLoad) || parsedLoad < 0)
            {
                _status.Text = "Informe uma carga em kg válida, usando o separador decimal do aparelho.";
                return;
            }
            load = parsedLoad;
        }
        int? reps = null;
        if (!string.IsNullOrWhiteSpace(_reps.Text))
        {
            if (!int.TryParse(_reps.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedReps) || parsedReps < 0)
            {
                _status.Text = "Informe um número válido de repetições.";
                return;
            }
            reps = parsedReps;
        }
        decimal? rir = null;
        if (!string.IsNullOrWhiteSpace(_rir.Text))
        {
            if (!decimal.TryParse(_rir.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedRir) || parsedRir < 0)
            {
                _status.Text = "Informe um RIR válido.";
                return;
            }
            rir = parsedRir;
        }
        if (load is null && reps is null)
        {
            _status.Text = "Informe a carga ou as repetições para salvar a série.";
            return;
        }

        try
        {
            if (_editingSetId is Guid setId)
            {
                _session = App.Data.UpdateSet(_session.Id, setId, load, reps, rir, EmptyToNull(_notes.Text));
                _status.Text = "Série atualizada.";
            }
            else
            {
                var snapshot = _plan.Exercises[_exercisePicker.SelectedIndex];
                _session = App.Data.AddSet(_session.Id, snapshot.Id, load, reps, rir, EmptyToNull(_notes.Text));
                _status.Text = $"Série {_session.Sets.Count(s => s.ExerciseId == snapshot.Id)} registrada.";
                StartRest(load?.ToString("0.##", CultureInfo.CurrentCulture));
            }
            ClearCorrection();
            RefreshSessionUi();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            _status.Text = $"Não foi possível salvar: {ex.Message}";
        }
    }

    private void ClearCorrection()
    {
        _editingSetId = null;
        _saveButton.Text = "Salvar série";
        _load.Text = _reps.Text = _rir.Text = _notes.Text = null;
    }

    private async Task PauseAndExitAsync()
    {
        if (_session is null) return;
        var hasPendingInput = !string.IsNullOrWhiteSpace(_load.Text) || !string.IsNullOrWhiteSpace(_reps.Text) ||
            !string.IsNullOrWhiteSpace(_rir.Text) || !string.IsNullOrWhiteSpace(_notes.Text);
        if (hasPendingInput)
        {
            var discard = await DisplayAlertAsync("Campos ainda não salvos",
                "Os valores digitados nesta série não serão registrados se você sair agora. Deseja descartar os campos e pausar?",
                "Descartar e pausar", "Continuar aqui");
            if (!discard) return;
            ClearCorrection();
        }
        PauseCurrentSession();
        await Shell.Current.Navigation.PopAsync();
    }

    private async Task ResumeCurrentSessionAsync()
    {
        if (_session is not { CompletedUtc: null, PausedUtc: not null }) return;
        var plan = new PlanDto(_session.PlanId, _session.PlanVersion, _day.Title, _session.PlanSnapshot);
        var reviews = App.Data.EvaluatePlan(plan);
        var blocked = reviews.Where(item => item.Evaluation.Decision is RestrictionDecision.Block or RestrictionDecision.RequiresReview).ToArray();
        if (blocked.Length > 0)
        {
            await DisplayAlertAsync("Revisão necessária",
                string.Join("\n", blocked.Select(item => item.Exercise.Name + ": é necessária uma revisão antes de retomar.")), "Entendi");
            return;
        }
        var warnings = reviews.Where(item => item.Evaluation.Decision == RestrictionDecision.Warn).ToArray();
        if (warnings.Length > 0)
        {
            var acknowledged = await DisplayAlertAsync("Aviso das suas restrições",
                string.Join("\n", warnings.Select(item => item.Exercise.Name + ": há um aviso registrado. Confirme para retomar.")),
                "Confirmar aviso", "Voltar");
            if (!acknowledged) return;
        }
        try
        {
            _session = App.Data.ResumeSession(_session.Id);
            PrefillLoadFromLastSet();
            _status.Text = "Sessão retomada.";
            RefreshSessionUi();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            _status.Text = $"Não foi possível retomar: {ex.Message}";
        }
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
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            _status.Text = $"Não foi possível pausar a sessão: {ex.Message}";
        }
    }

    private async Task FinishAsync()
    {
        if (_session is null) return;
        if (_session.PausedUtc is not null)
        {
            await DisplayAlertAsync("Retome o treino", "Retome a sessão antes de concluí-la.", "OK");
            return;
        }
        var confirm = await DisplayAlertAsync("Concluir treino?", "As séries registradas serão mantidas.", "Concluir", "Continuar");
        if (!confirm) return;
        var elapsedSeconds = Math.Max(0, (DateTimeOffset.UtcNow - _session.StartedUtc).TotalSeconds - _session.TotalPausedSeconds);
        var minutes = Math.Max(0, (int)Math.Round(elapsedSeconds / 60));
        try
        {
            _finishing = true;
            var completed = App.Data.CompleteSession(_session.Id, minutes, null);
            _session = completed;
            _timer.Stop();
            var exerciseCount = completed.Sets.Select(set => set.ExerciseId).Distinct().Count();
            await DisplayAlertAsync("Treino salvo",
                $"{exerciseCount} exercício(s) · {completed.Sets.Length} série(s) · {minutes} min. Registro salvo neste aparelho.",
                "OK");
            await Shell.Current.Navigation.PopAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            _finishing = false;
            _status.Text = $"Não foi possível concluir: {ex.Message}";
        }
    }

    private void StartRest(string? nextLoad)
    {
        if (!int.TryParse(_restSeconds.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds is < 1 or > 7200)
        {
            _status.Text = "O descanso precisa ficar entre 1 e 7200 segundos.";
            return;
        }

        var exercise = _plan.Exercises[_exercisePicker.SelectedIndex];
        var completedSets = _session?.Sets.Count(set => set.ExerciseId == exercise.Id) ?? 0;
        _queuedLoad = nextLoad;
        _isResting = true;
        _restHeading.Text = "DESCANSO";
        _restNextSeries.Text = $"Próxima: série {completedSets + 1}";
        _restButton.Text = "Encerrar descanso";
        _restDeadline = DateTimeOffset.UtcNow.AddSeconds(seconds);
        RefreshSessionUi();
        UpdateCountdown();
        _timer.Start();
    }

    private void EndRest()
    {
        _timer.Stop();
        _restDeadline = null;
        _isResting = false;
        _restCountdown.Text = "00:00";
        _load.Text = _queuedLoad;
        _queuedLoad = null;
        _status.Text = "Descanso encerrado. Ajuste a carga e registre a próxima série.";
        RefreshSessionUi();
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

    private void UpdateCountdown()
    {
        if (_restDeadline is null) return;
        var remaining = Math.Max(0, (int)Math.Ceiling((_restDeadline.Value - DateTimeOffset.UtcNow).TotalSeconds));
        _restCountdown.Text = $"{remaining / 60:00}:{remaining % 60:00}";
        if (remaining == 0)
        {
            EndRest();
            _status.Text = "Descanso concluído. Próxima série liberada.";
        }
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
            var existing = catalog.FirstOrDefault(x => x.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
            return existing is null
                ? new ExerciseSnapshotDto(StableId($"{sheet.Id:N}:{day.Key}:{item.Order}:{item.Name}"), 1, item.Name, [], [])
                : new ExerciseSnapshotDto(existing.Id, existing.Version, existing.Name, existing.MovementPatterns, existing.Equipment,
                    existing.UsesExternalLoad, existing.Region, existing.Position, existing.RangeOfMotion);
        }).ToArray();
        return new PlanDto(StableId($"{sheet.Id:N}:{day.Key}:plan"), 1, day.Title, snapshots);
    }

    private static Guid StableId(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
