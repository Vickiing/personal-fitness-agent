using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class MainPage : ContentPage
{
    private readonly Label _summary = Ui.Body("");

    public MainPage()
    {
        Title = "Hoje";
        BackgroundColor = Color.FromArgb("#F5F4EF");
        Content = Ui.Scroll("Seu acompanhamento", new Label
        {
            Text = "Registre o que aconteceu. Os dados ficam neste aparelho.",
            FontSize = 16,
            TextColor = Color.FromArgb("#64716E")
        }, Ui.Section("Visão da semana"), _summary,
            Ui.Section("Atalhos"),
            Ui.Button("Visualizar minha ficha A/B", async (_, _) => await Shell.Current.GoToAsync(nameof(TrainingSheetPage))),
            Ui.Button("Registrar treino", async (_, _) => await Shell.Current.GoToAsync("//workout")),
            Ui.Button("Fazer check-in", async (_, _) => await Shell.Current.GoToAsync("//checkin"), true),
            Ui.Button("Ver restrições", async (_, _) => await Shell.Current.GoToAsync("//restrictions"), true));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var summary = App.Data.WeeklyReview(DateOnly.FromDateTime(DateTime.Now));
            var adherence = summary.AdherencePercent is null ? "indisponível (agenda não configurada)" : $"{summary.AdherencePercent:0}%";
            _summary.Text = $"{summary.WorkoutCount} treinos concluídos\nAdesão: {adherence}\n" +
                $"Sono mediano: {summary.MedianSleepHours?.ToString("0.0") + " h" ?? "sem dados"}\n" +
                $"Peso mediano: {summary.MedianWeightKg?.ToString("0.0") + " kg" ?? "sem dados"}\n" +
                $"Sintomas registrados: {summary.SymptomCount}\n\n" + string.Join("\n", summary.DataQualityWarnings);
        }
        catch (Exception error) { _summary.Text = $"Os registros permanecem no aparelho. Não foi possível carregar o resumo: {error.Message}"; }
    }
}
