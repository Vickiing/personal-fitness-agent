using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public sealed class ExerciseDetailPage : ContentPage
{
    public ExerciseDetailPage(string dayKey, WorkoutSheetExercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        var guide = ExerciseGuideCatalog.For(exercise.Name);
        Title = "Detalhe";
        BackgroundColor = Color.FromArgb("#F5F4EF");
        var steps = new VerticalStackLayout { Spacing = 10 };
        for (var index = 0; index < guide.Steps.Count; index++)
        {
            var number = new Label
            {
                Text = (index + 1).ToString(),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#145A50"),
                WidthRequest = 34,
                HeightRequest = 34,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var stepText = new Label
            {
                Text = guide.Steps[index],
                FontSize = 16,
                TextColor = Color.FromArgb("#202A29"),
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill
            };
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(number, 0, 0);
            row.Add(stepText, 1, 0);
            steps.Children.Add(row);
        }

        var content = Ui.Form(exercise.Name,
            Ui.Body(string.IsNullOrWhiteSpace(dayKey) ? "Detalhe do exercício" : $"Treino {dayKey} · exercício {exercise.Order}"),
            new ExerciseDemoView(guide),
            Ui.Section("Séries, repetições e descanso"),
            Ui.Body($"{Display(exercise.Sets, "Séries não informadas")} · {Display(exercise.Repetitions, "Repetições não informadas")} · {Display(exercise.Rest, "Descanso não informado")}"),
            Ui.Section("Passo a passo"),
            steps);

        if (!string.IsNullOrWhiteSpace(exercise.Instructions))
        {
            content.Children.Add(Ui.Section("Observação da sua ficha"));
            content.Children.Add(Ui.Body(exercise.Instructions));
        }
        if (!string.IsNullOrWhiteSpace(exercise.Priority))
        {
            content.Children.Add(Ui.Section("Prioridade na ficha"));
            content.Children.Add(Ui.Body(exercise.Priority));
        }
        if (!string.IsNullOrWhiteSpace(exercise.ReferenceLoad))
        {
            content.Children.Add(Ui.Section("Referência anotada"));
            content.Children.Add(Ui.Body($"{exercise.ReferenceLoad}. É uma anotação da ficha, não uma carga prescrita."));
        }
        content.Children.Add(Ui.Body("A demonstração é ilustrativa e local. Siga as limitações e orientações registradas na sua ficha."));
        Content = new ScrollView { Content = content, BackgroundColor = Color.FromArgb("#F5F4EF") };
    }

    private static string Display(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
