using PersonalFitnessAgent.Core.Application;
using PersonalFitnessAgent.Infrastructure.Sqlite;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class ProgressPage : ContentPage
{
    private readonly Label _summary = Ui.Body("");
    private readonly (DayOfWeek Day, CheckBox Control)[] _scheduleDays = Enum.GetValues<DayOfWeek>()
        .Select(day => (day, new CheckBox())).ToArray();

    public ProgressPage()
    {
        Title = "Progresso";
        var content = Ui.Form("Resumo semanal", _summary, Ui.Section("Agenda semanal"),
            Ui.Body("Marque os dias em que costuma planejar um treino. A adesao conta apenas os dias planejados que ja passaram."));
        foreach (var item in _scheduleDays)
            content.Children.Add(new HorizontalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                Children = { item.Control, Ui.Body(DayName(item.Day)) }
            });
        content.Children.Add(Ui.Button("Salvar agenda semanal", (_, _) => SaveSchedule()));
        content.Children.Add(Ui.Section("Exportar e restaurar"));
        content.Children.Add(Ui.Body("O backup inclui registros e auditoria. O arquivo pode conter informações pessoais de saúde; escolha um destino privado."));
        content.Children.Add(Ui.Button("Exportar backup JSON", async (_, _) => await ExportBackup()));
        content.Children.Add(Ui.Button("Exportar backup criptografado", async (_, _) => await ExportEncryptedBackup(), true));
        content.Children.Add(Ui.Button("Exportar tabelas CSV", async (_, _) => await ExportCsv(), true));
        content.Children.Add(Ui.Button("Importar backup com prévia", async (_, _) => await ImportBackup(), true));
        content.Children.Add(Ui.Section("Privacidade"));
        content.Children.Add(Ui.Button("Apagar todos os registros deste aparelho", async (_, _) => await DeleteAll(), true));
        Content = new ScrollView { Content = content, BackgroundColor = Color.FromArgb("#F5F4EF") };
        Refresh();
    }

    protected override void OnAppearing() { base.OnAppearing(); Refresh(); }

    private void Refresh()
    {
        var configured = App.Data.Schedule?.PlannedWeekdays.ToHashSet() ?? [];
        foreach (var (day, control) in _scheduleDays) control.IsChecked = configured.Contains(day);
        var report = App.Data.WeeklyReview(DateOnly.FromDateTime(DateTime.Now));
        var trends = report.ExerciseTrends.Count == 0 ? "Sem séries de treino nesta semana." :
            string.Join("\n", report.ExerciseTrends.Select(trend =>
                $"{App.Data.Exercises.FirstOrDefault(exercise => exercise.Id == trend.ExerciseId)?.Name ?? "Exercício"}: " +
                $"{trend.ObservationCount} série(s), melhor carga {trend.BestLoadKg?.ToString("0.##") ?? "desconhecida"} kg"));
        _summary.Text = $"{report.StartDate:dd/MM} a {report.EndDate:dd/MM}\n" +
            $"Treinos: {report.WorkoutCount}\nAdesão: {report.AdherencePercent?.ToString("0") + "%" ?? "sem agenda definida"}\n" +
            $"Sono mediano: {report.MedianSleepHours?.ToString("0.0") + " h" ?? "sem dados"}\n" +
            $"Peso mediano: {report.MedianWeightKg?.ToString("0.0") + " kg" ?? "sem dados"}\n" +
            $"Variacao de peso em 28 dias: {report.WeightChangeKg28Days?.ToString("+0.0;-0.0;0.0") + " kg" ?? "dados insuficientes"} " +
            $"(medidas por periodo: {report.EarlierWeightCount}/3 e {report.RecentWeightCount}/3)\n" +
            $"Cintura mediana: {report.MedianWaistCm?.ToString("0.0") + " cm" ?? "sem dados"}\n" +
            $"Sintomas registrados: {report.SymptomCount}\n\n{trends}\n\n" +
            string.Join("\n", report.DataQualityWarnings);
    }

    private async void SaveSchedule()
    {
        try
        {
            App.Data.SaveSchedule(_scheduleDays.Where(item => item.Control.IsChecked).Select(item => item.Day));
            Refresh();
            await DisplayAlertAsync("Agenda salva", "A agenda semanal foi salva neste aparelho.", "OK");
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private static string DayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "Domingo", DayOfWeek.Monday => "Segunda-feira", DayOfWeek.Tuesday => "Terca-feira",
        DayOfWeek.Wednesday => "Quarta-feira", DayOfWeek.Thursday => "Quinta-feira", DayOfWeek.Friday => "Sexta-feira",
        DayOfWeek.Saturday => "Sabado", _ => throw new ArgumentOutOfRangeException(nameof(day))
    };

    private async Task ExportBackup()
    {
        try
        {
            var confirm = await DisplayAlertAsync("Dados sensíveis", "O backup JSON inclui seus registros e histórico de auditoria. Salve ou compartilhe apenas em um destino privado.", "Continuar", "Cancelar");
            if (!confirm) return;
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(path, App.Data.ExportJson());
            await Share.Default.RequestAsync(new ShareFileRequest("Backup Fitness Agent", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ExportCsv()
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            await File.WriteAllTextAsync(path, App.Data.ExportCsv(), new System.Text.UTF8Encoding(true));
            await Share.Default.RequestAsync(new ShareFileRequest("Exportação Fitness Agent", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ExportEncryptedBackup()
    {
        try
        {
            var first = await PassphrasePage.Request(this, "Criptografar backup", "Crie uma senha que você conseguirá guardar em local seguro.");
            if (first is null) return;
            var second = await PassphrasePage.Request(this, "Confirmar senha", "Digite a mesma senha novamente.");
            if (second is null) return;
            if (!StringComparer.Ordinal.Equals(first, second))
            {
                await DisplayAlertAsync("Senhas diferentes", "A senha de confirmação não corresponde.", "OK");
                return;
            }
            var path = Path.Combine(FileSystem.CacheDirectory, $"fitness-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pfa.json");
            await File.WriteAllTextAsync(path, App.Data.ExportEncrypted(first));
            await Share.Default.RequestAsync(new ShareFileRequest("Backup criptografado Fitness Agent", new ShareFile(path)));
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task ImportBackup()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Selecione um backup JSON" });
            if (file is null) return;
            var json = await File.ReadAllTextAsync(file.FullPath);
            if (BackupEncryption.IsEncrypted(json))
            {
                var passphrase = await PassphrasePage.Request(this, "Descriptografar backup", "Digite a senha usada na exportação.");
                if (passphrase is null) return;
                json = BackupEncryption.Decrypt(json, passphrase);
            }
            var preview = App.Data.PreviewImport(json);
            if (!preview.IsValid)
            {
                await DisplayAlertAsync("Backup inválido", preview.Error ?? "O arquivo não pode ser restaurado.", "OK");
                return;
            }
            var approved = await DisplayAlertAsync("Prévia do backup",
                $"Versão {preview.FormatVersion}: {preview.Records} registros, {preview.AuditEvents} eventos de auditoria e {preview.OutboxEvents} eventos pendentes.\n\nA restauração substitui os dados locais atuais. Continuar?",
                "Restaurar", "Cancelar");
            if (!approved) return;
            App.Data.Restore(json);
            Refresh();
            await DisplayAlertAsync("Restaurado", "Os registros e os IDs de origem foram restaurados.", "OK");
        }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }

    private async Task DeleteAll()
    {
        var approved = await DisplayAlertAsync("Apagar dados locais", "Isso remove registros, restrições, auditoria e fila de sincronização deste aparelho. Esta ação não pode ser desfeita sem um backup.", "Apagar tudo", "Cancelar");
        if (!approved) return;
        try { App.Data.DeleteAllData(); Refresh(); }
        catch (Exception e) { await Ui.ShowError(this, e); }
    }
}
