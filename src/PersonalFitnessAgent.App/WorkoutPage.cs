using PersonalFitnessAgent.Core.Domain;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class WorkoutPage : ContentPage
{
    private readonly Entry _exerciseName = Ui.Input("Nome do exercício");
    private readonly Entry _patterns = Ui.Input("Padrões de movimento (separe por vírgula)");
    private readonly Entry _equipment = Ui.Input("Equipamentos (separe por vírgula)");
    private readonly Picker _externalLoad = new() { Title = "Carga externa" };
    private readonly Entry _region = Ui.Input("Regiao corporal (opcional)");
    private readonly Entry _position = Ui.Input("Posicao corporal (opcional)");
    private readonly Entry _rangeOfMotion = Ui.Input("Amplitude de movimento (opcional)");
    private readonly Picker _exercisePicker = new() { Title = "Selecione um exercício" };
    private readonly HashSet<Guid> _draftExerciseIds = [];
    private readonly Label _draftLabel = Ui.Body("Nenhum exercício no plano novo.");
    private readonly Entry _planName = Ui.Input("Nome do plano (ex.: A ou B)");
    private readonly Picker _planPicker = new() { Title = "Selecione um plano" };
    private readonly Entry _load = Ui.Input("Carga em kg", Keyboard.Numeric);
    private readonly Entry _reps = Ui.Input("Repetições", Keyboard.Numeric);
    private readonly Entry _rir = Ui.Input("RIR (opcional)", Keyboard.Numeric);
    private readonly Entry _setNotes = Ui.Input("Observação (opcional)");
    private readonly Entry _duration = Ui.Input("Duração em minutos (opcional)", Keyboard.Numeric);
    private readonly Label _status = Ui.Body("Crie exercícios e planos para iniciar um treino.");
    private SessionDto? _session;

    public WorkoutPage()
    {
        Title = "Treino";
        _externalLoad.ItemsSource = new[] { "Desconhecida", "Sem carga externa", "Com carga externa" };
        _externalLoad.SelectedIndex = 0;
        _exercisePicker.ItemDisplayBinding = new Binding(nameof(ExerciseDto.Name));
        _planPicker.ItemDisplayBinding = new Binding(nameof(PlanDto.Label));
        var setExercise = Ui.Button("Adicionar ao plano", (_, _) => AddExerciseToDraft(), true);
        var createPlan = Ui.Button("Salvar plano", (_, _) => CreatePlan());
        var addSet = Ui.Button("Salvar série", (_, _) => AddSet());
        var complete = Ui.Button("Concluir treino", (_, _) => Complete());
        _exercisePicker.SelectedIndexChanged += (_, _) => UpdateDraftLabel();
        _exercisePicker.SelectedIndexChanged += (_, _) => LoadExerciseForEdit();
        _planPicker.SelectedIndexChanged += (_, _) => ToggleSessionControls();
        var content = Ui.Form("Treino manual e offline",
            Ui.Body("Cadastre os movimentos e versões dos planos. Cada sessão mantém a versão que foi iniciada."),
            Ui.Section("Catálogo"), _exerciseName, _patterns, _equipment, _externalLoad, _region, _position, _rangeOfMotion,
            Ui.Button("Salvar nova versão do exercício", (_, _) => SaveExercise(), true), _exercisePicker,
            setExercise, _draftLabel, _planName, createPlan,
            Ui.Section("Registrar sessão"), _planPicker,
            Ui.Button("Iniciar treino", (_, _) => Start(), true),
            _load, _reps, _rir, _setNotes, addSet, _duration, complete, _status);
        Content = new ScrollView { Content = content, BackgroundColor = Color.FromArgb("#F5F4EF") };
        RefreshChoices();
    }

    private async void SaveExercise()
    {
        try
        {
            var selected = _exercisePicker.SelectedItem as ExerciseDto;
            bool? externalLoad = _externalLoad.SelectedIndex switch { 1 => false, 2 => true, _ => null };
            var saved = App.Data.SaveExercise(_exerciseName.Text ?? "", _patterns.Text ?? "", _equipment.Text ?? "", selected?.Id,
                externalLoad, _region.Text, _position.Text, _rangeOfMotion.Text);
            RefreshChoices(); _exercisePicker.SelectedItem = App.Data.Exercises.FirstOrDefault(item => item.Id == saved.Id);
            _status.Text = $"Exercício salvo como versão {saved.Version}; sessões anteriores mantêm seus registros.";
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private void RefreshChoices()
    {
        _exercisePicker.ItemsSource = App.Data.Exercises.ToArray();
        _planPicker.ItemsSource = App.Data.Plans.ToArray();
        _planPicker.SelectedItem = App.Data.NextPlan;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshChoices();
        if (_session is not null) return;
        _session = App.Data.Sessions.Where(session => session.CompletedUtc is null)
            .OrderByDescending(session => session.StartedUtc).FirstOrDefault();
        if (_session is not null)
            _status.Text = "Sessão em andamento recuperada do armazenamento local.";
    }

    private void AddExerciseToDraft()
    {
        if (_exercisePicker.SelectedItem is not ExerciseDto exercise) { _status.Text = "Selecione um exercício."; return; }
        _draftExerciseIds.Add(exercise.Id);
        UpdateDraftLabel();
    }

    private void UpdateDraftLabel() => _draftLabel.Text = _draftExerciseIds.Count == 0
        ? "Nenhum exercício no plano novo."
        : $"{_draftExerciseIds.Count} exercício(s) selecionado(s) para a próxima versão do plano.";

    private void LoadExerciseForEdit()
    {
        if (_exercisePicker.SelectedItem is not ExerciseDto exercise) return;
        _exerciseName.Text = exercise.Name;
        _patterns.Text = string.Join(", ", exercise.MovementPatterns);
        _equipment.Text = string.Join(", ", exercise.Equipment);
        _externalLoad.SelectedIndex = exercise.UsesExternalLoad switch { false => 1, true => 2, _ => 0 };
        _region.Text = exercise.Region;
        _position.Text = exercise.Position;
        _rangeOfMotion.Text = exercise.RangeOfMotion;
    }

    private async void CreatePlan()
    {
        try
        {
            var warnings = App.Data.EvaluatePlan(_draftExerciseIds)
                .Where(result => result.Evaluation.Decision == RestrictionDecision.Warn).ToArray();
            if (warnings.Length > 0)
            {
                var accepted = await DisplayAlertAsync("Política de aviso",
                    string.Join("\n", warnings.Select(item => $"{item.Exercise.Name}: {string.Join("; ", item.Evaluation.Reasons)}")),
                    "Continuar", "Cancelar");
                if (!accepted) return;
            }
            var plan = App.Data.SavePlan(_planName.Text ?? "", _draftExerciseIds);
            _draftExerciseIds.Clear(); _planName.Text = ""; RefreshChoices();
            _status.Text = $"Plano {plan.Label} v{plan.Version} salvo.";
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void Start()
    {
        if (_session is not null) { _status.Text = "Conclua a sessão atual antes de iniciar outra."; return; }
        if (_planPicker.SelectedItem is not PlanDto plan) { _status.Text = "Selecione um plano."; return; }
        try
        {
            _session = App.Data.StartSession(plan);
            _status.Text = $"Treino iniciado: {plan.Label} v{plan.Version}.";
            ToggleSessionControls();
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void AddSet()
    {
        if (_session is null) { _status.Text = "Inicie um treino antes de registrar séries."; return; }
        if (_exercisePicker.SelectedItem is not ExerciseDto exercise) { _status.Text = "Selecione o exercício da série no catálogo."; return; }
        try
        {
            var set = App.Data.AddSet(_session.Id, exercise.Id, Ui.Decimal(_load.Text, "Carga"), Ui.Integer(_reps.Text, "Repetições"), Ui.Decimal(_rir.Text, "RIR"), _setNotes.Text);
            _status.Text = $"Série salva. Total nesta sessão: {set.Sets.Length}.";
            _load.Text = _reps.Text = _rir.Text = _setNotes.Text = "";
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void Complete()
    {
        if (_session is null) { _status.Text = "Nenhum treino em andamento."; return; }
        try
        {
            var completed = App.Data.CompleteSession(_session.Id, Ui.Integer(_duration.Text, "Duração"), null);
            _session = null; _duration.Text = "";
            _status.Text = $"Treino concluído com {completed.Sets.Length} série(s).";
            RefreshChoices();
            ToggleSessionControls();
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private void ToggleSessionControls() { }
}
