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
        Content = new ScrollView { Content = content, BackgroundColor = Color.FromArgb("#0B0F14") };
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


}
