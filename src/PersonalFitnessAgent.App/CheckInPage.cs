using PersonalFitnessAgent.Core.Domain;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class CheckInPage : ContentPage
{
    private readonly Entry _sleep = Ui.Input("Sono em horas (opcional)", Keyboard.Numeric);
    private readonly Entry _weight = Ui.Input("Peso em kg (opcional)", Keyboard.Numeric);
    private readonly Entry _waist = Ui.Input("Cintura em cm (opcional)", Keyboard.Numeric);
    private readonly DatePicker _recordDate = new() { Date = DateTime.Today, MaximumDate = DateTime.Today };
    private readonly Picker _activity = new() { Title = "Nível de atividade (opcional)" };
    private readonly Picker _measurementSource = new() { Title = "Origem das medidas" };
    private readonly Entry _food = Ui.Input("Alimentação (texto; sem calorias estimadas)");
    private readonly Picker _foodCatalog = new() { Title = "Alimento do catálogo brasileiro (opcional)" };
    private readonly Entry _foodGrams = Ui.Input("Peso em gramas (opcional)", Keyboard.Numeric);
    private readonly Entry _symptom = Ui.Input("Sintoma percebido (opcional)");
    private readonly Entry _region = Ui.Input("Região relacionada (opcional)");
    private readonly Entry _severity = Ui.Input("Intensidade de 1 a 10 (opcional)", Keyboard.Numeric);
    private readonly Entry _patterns = Ui.Input("Padrões de movimento relacionados, separados por vírgula");
    private readonly DatePicker _symptomOnset = new() { MaximumDate = DateTime.Today };
    private readonly Picker _relatedSession = new() { Title = "Sessão relacionada (opcional)" };
    private readonly Label _status = Ui.Body("");
    private readonly VerticalStackLayout _foodEntries = new() { Spacing = 6 };
    private readonly VerticalStackLayout _symptomEntries = new() { Spacing = 6 };
    private Guid? _editingFoodId;
    private Guid? _editingCatalogFoodId;
    private Guid? _editingSymptomId;

    public CheckInPage()
    {
        Title = "Check-in";
        var date = LogDate;
        _recordDate.DateSelected += (_, _) => RefreshSelectedDate();
        _activity.ItemsSource = new[] { "Não informado", "Sedentário", "Leve", "Moderado", "Alto", "Muito alto" };
        _activity.SelectedIndex = 0;
        _measurementSource.ItemsSource = new[] { "Relato pessoal", "Medida direta" };
        _measurementSource.SelectedIndex = 0;
        _foodCatalog.ItemsSource = FoodCatalog.Items.ToArray();
        _foodCatalog.ItemDisplayBinding = new Binding(nameof(FoodCatalogItem.Name));
        var existing = App.Data.DailyLogs.FirstOrDefault(log => log.LocalDate == date);
        if (existing is not null)
        {
            _sleep.Text = existing.SleepHours?.ToString(System.Globalization.CultureInfo.CurrentCulture);
            _weight.Text = existing.WeightKg?.ToString(System.Globalization.CultureInfo.CurrentCulture);
            _waist.Text = existing.WaistCm?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }
        _relatedSession.ItemsSource = App.Data.Sessions.OrderByDescending(session => session.StartedUtc).Take(30).ToArray();
        _relatedSession.ItemDisplayBinding = new Binding(nameof(SessionDto.StartedAtLocal), stringFormat: "{0:dd/MM HH:mm}");
        Content = Ui.Scroll("Check-in diário",
            Ui.Body("Deixe em branco o que você não sabe. Registro manual disponível sem conexão."),
            Ui.Section("Data do registro"), _recordDate, _sleep, _activity, _weight, _waist, _measurementSource,
            Ui.Button("Atualizar medidas registradas (campos vazios ficam desconhecidos)", (_, _) => UpdateMeasurements(), true),
            Ui.Section("Alimentação"), Ui.Body("Texto livre nunca recebe calorias inventadas. O catálogo usa valores por 100 g; sem peso informado, a energia permanece desconhecida."),
            _food, Ui.Button("Salvar descrição sem quantificar", (_, _) => SaveFoodNote(), true),
            _foodCatalog, _foodGrams, Ui.Button("Adicionar alimento do catálogo", (_, _) => SaveCatalogFood()),
            Ui.Section("Alimentos desta data"), _foodEntries,
            Ui.Section("Sintoma"), Ui.Body("Você pode registrar o sintoma depois do treino e vincular à sessão anterior."),
            _symptom, _region, _severity, _patterns, _symptomOnset, _relatedSession,
            Ui.Button("Salvar check-in", (_, _) => Save()), Ui.Section("Sintomas desta data"), _symptomEntries, _status);
        RefreshEntries();
    }

    private async void Save()
    {
        try
        {
            var hours = Ui.Decimal(_sleep.Text, "Sono");
            if (hours > 24) throw new FormatException("Sono deve estar entre 0 e 24 horas.");
            var severity = Ui.Integer(_severity.Text, "Intensidade");
            if (severity is < 1 or > 10) throw new FormatException("Intensidade deve estar entre 1 e 10.");
            var weight = Ui.Decimal(_weight.Text, "Peso");
            var waist = Ui.Decimal(_waist.Text, "Cintura");
            var activity = _activity.SelectedIndex == 0 ? ActivityLevel.Unknown : (ActivityLevel)_activity.SelectedIndex;
            var source = _measurementSource.SelectedIndex == 1 ? QuantitySource.Measured : QuantitySource.SelfReported;
            if (_editingSymptomId is Guid editId)
            {
                App.Data.UpdateDailyMeasurements(LogDate, hours, activity, weight, waist, source);
                App.Data.UpdateSymptomEntry(LogDate, editId, _symptom.Text ?? "",
                    _region.Text ?? "", severity, DateOnly.FromDateTime(_symptomOnset.Date ?? DateTime.Today),
                    (_relatedSession.SelectedItem as SessionDto)?.Id, _patterns.Text);
                _editingSymptomId = null;
                _food.Text = _symptom.Text = _region.Text = _severity.Text = _patterns.Text = "";
                _status.Text = "Sintoma corrigido.";
                RefreshEntries();
                return;
            }
            var log = App.Data.SaveCheckIn(LogDate, hours, weight, waist, _food.Text,
                _symptom.Text, _region.Text, severity, _patterns.Text,
                DateOnly.FromDateTime(_symptomOnset.Date ?? DateTime.Today), (_relatedSession.SelectedItem as SessionDto)?.Id,
                _activity.SelectedIndex == 0 ? null : _activity.SelectedIndex, source);
            _food.Text = _symptom.Text = _region.Text = _severity.Text = _patterns.Text = "";
            _status.Text = $"Check-in salvo. {log.Foods.Length} alimento(s) e {log.Symptoms.Length} sintoma(s).";
            RefreshEntries();
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void SaveFoodNote()
    {
        if (string.IsNullOrWhiteSpace(_food.Text)) { _status.Text = "Descreva o alimento para salvar uma nota qualitativa."; return; }
        try
        {
            if (_editingFoodId is Guid foodId)
            {
                App.Data.RenameFoodEntry(LogDate, foodId, _food.Text.Trim());
                _editingFoodId = null;
                _food.Text = "";
                _status.Text = "Alimento corrigido.";
                RefreshEntries();
                return;
            }
            App.Data.SaveCheckIn(LogDate, null, null, null,
                _food.Text, null, null, null, null);
            _food.Text = "";
            _status.Text = "Descrição salva sem porção ou calorias estimadas.";
            RefreshEntries();
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void UpdateMeasurements()
    {
        try
        {
            var activity = _activity.SelectedIndex <= 0 ? ActivityLevel.Unknown : (ActivityLevel)_activity.SelectedIndex;
            var updated = App.Data.UpdateDailyMeasurements(LogDate,
                Ui.Decimal(_sleep.Text, "Sono"), activity, Ui.Decimal(_weight.Text, "Peso"),
                Ui.Decimal(_waist.Text, "Cintura"),
                _measurementSource.SelectedIndex == 1 ? QuantitySource.Measured : QuantitySource.SelfReported);
            _status.Text = $"Medidas atualizadas. Sono: {updated.SleepHours?.ToString("0.##") ?? "desconhecido"}; peso: {updated.WeightKg?.ToString("0.##") ?? "desconhecido"} kg; cintura: {updated.WaistCm?.ToString("0.##") ?? "desconhecida"} cm.";
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async void SaveCatalogFood()
    {
        try
        {
            var grams = Ui.Decimal(_foodGrams.Text, "Peso do alimento");
            if (_editingCatalogFoodId is Guid foodId)
            {
                var updated = App.Data.UpdateCatalogFoodQuantity(LogDate, foodId, grams);
                _editingCatalogFoodId = null;
                _foodGrams.Text = "";
                RefreshEntries();
                var corrected = updated.Foods.Single(food => food.Id == foodId);
                _status.Text = corrected.EnergyKcal is null
                    ? $"{corrected.Name} atualizado sem quantidade; energia desconhecida."
                    : $"Porção corrigida. Energia estimada: {corrected.EnergyKcal:0.#} kcal.";
                return;
            }
            if (_foodCatalog.SelectedItem is not FoodCatalogItem item) { _status.Text = "Selecione um alimento do catálogo."; return; }
            App.Data.SaveCatalogFood(LogDate, item, grams);
            _foodGrams.Text = "";
            RefreshEntries();
            var energy = FoodCatalog.EstimateEnergy(item, grams);
            _status.Text = grams is null
                ? $"{item.Name} salvo sem quantidade; calorias desconhecidas."
                : $"{item.Name} salvo. Energia estimada: {energy!.Value:0.#} kcal (TACO, estimativa).";
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }
    private DateOnly LogDate => DateOnly.FromDateTime(_recordDate.Date ?? DateTime.Today);

    private void RefreshSelectedDate()
    {
        var log = App.Data.DailyLogs.FirstOrDefault(item => item.LocalDate == LogDate);
        _sleep.Text = log?.SleepHours?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        _weight.Text = log?.WeightKg?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        _waist.Text = log?.WaistCm?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        _activity.SelectedIndex = log?.Activity ?? 0;
        _food.Text = _symptom.Text = _region.Text = _severity.Text = _patterns.Text = "";
        _foodGrams.Text = "";
        _foodCatalog.SelectedItem = null;
        _editingFoodId = _editingCatalogFoodId = _editingSymptomId = null;
        RefreshEntries();
    }

    private void RefreshEntries()
    {
        var today = LogDate;
        var log = App.Data.DailyLogs.FirstOrDefault(item => item.LocalDate == today);
        _foodEntries.Children.Clear();
        foreach (var food in log?.Foods ?? [])
        {
            var row = new HorizontalStackLayout { Spacing = 6 };
            row.Children.Add(Ui.Body(food.Quantified ? $"{food.Name} - {food.Description}" : $"{food.Name} - sem quantidade"));
            row.Children.Add(Ui.Button("Editar nome", (_, _) => { _editingCatalogFoodId = null; _editingFoodId = food.Id; _food.Text = food.Name; _status.Text = "Edite o nome e salve a nota."; }));
            if (food.CatalogCode is not null && FoodCatalog.Items.Any(item => item.Code == food.CatalogCode))
                row.Children.Add(Ui.Button("Editar porção", (_, _) => LoadFoodForPortionEdit(food), true));
            row.Children.Add(Ui.Button("Remover", async (_, _) => await DeleteFood(food.Id), true));
            _foodEntries.Children.Add(row);
        }
        if (_foodEntries.Children.Count == 0) _foodEntries.Children.Add(Ui.Body("Nenhum alimento registrado hoje."));

        _symptomEntries.Children.Clear();
        foreach (var symptom in log?.Symptoms ?? [])
        {
            var row = new HorizontalStackLayout { Spacing = 6 };
            row.Children.Add(Ui.Body($"{symptom.Region}: {symptom.Description} - intensidade {symptom.Severity?.ToString() ?? "desconhecida"}"));
            row.Children.Add(Ui.Button("Editar", (_, _) => LoadSymptomForEdit(symptom)));
            row.Children.Add(Ui.Button("Remover", async (_, _) => await DeleteSymptom(symptom.Id), true));
            _symptomEntries.Children.Add(row);
        }
        if (_symptomEntries.Children.Count == 0) _symptomEntries.Children.Add(Ui.Body("Nenhum sintoma registrado hoje."));
    }

    private void LoadFoodForPortionEdit(FoodDto food)
    {
        _editingFoodId = null;
        _editingCatalogFoodId = food.Id;
        _foodCatalog.SelectedItem = FoodCatalog.Items.First(item => item.Code == food.CatalogCode);
        _foodGrams.Text = food.Amount?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        _status.Text = "Corrija os gramas. Deixe em branco para marcar a quantidade como desconhecida.";
    }

    private void LoadSymptomForEdit(SymptomDto symptom)
    {
        _editingSymptomId = symptom.Id;
        _symptom.Text = symptom.Description;
        _region.Text = symptom.Region;
        _severity.Text = symptom.Severity?.ToString(System.Globalization.CultureInfo.CurrentCulture);
        _patterns.Text = string.Join(", ", symptom.MovementPatterns);
        _symptomOnset.Date = symptom.OnsetUtc?.ToOffset(TimeSpan.FromMinutes(symptom.OnsetOffsetMinutes ?? 0)).DateTime ?? DateTime.Today;
        _relatedSession.SelectedItem = symptom.RelatedSessionId is Guid id
            ? App.Data.Sessions.FirstOrDefault(session => session.Id == id) : null;
        _status.Text = "Corrija os campos do sintoma e salve o check-in.";
    }

    private async Task DeleteFood(Guid id)
    {
        if (!await DisplayAlertAsync("Remover alimento", "Remover este alimento registrado?", "Remover", "Cancelar")) return;
        try { App.Data.DeleteFoodEntry(LogDate, id); RefreshEntries(); }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task DeleteSymptom(Guid id)
    {
        if (!await DisplayAlertAsync("Remover sintoma", "Remover este sintoma registrado?", "Remover", "Cancelar")) return;
        try { App.Data.DeleteSymptomEntry(LogDate, id); RefreshEntries(); }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }
}
