using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public sealed class ExerciseDetailPage : ContentPage
{
    public ExerciseDetailPage(string dayKey, WorkoutSheetExercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        var guide = ExerciseGuideCatalog.For(exercise.Name);
        Title = "Movimento";
        BackgroundColor = Color.FromArgb("#F4F6F2");

        var eyebrow = new Label
        {
            Text = string.IsNullOrWhiteSpace(dayKey) ? "DEMONSTRAÇÃO" : $"TREINO {dayKey.ToUpperInvariant()}  ·  MOVIMENTO {exercise.Order:00}",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#71807B"),
            CharacterSpacing = 1.5
        };
        var title = new Label
        {
            Text = exercise.Name,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#14221F"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
        var demo = new ExerciseDemoView(guide);

        var prescription = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8,
            Children =
            {
                Metric("SÉRIES", Display(exercise.Sets, "—")),
                Metric("REPETIÇÕES", Display(exercise.Repetitions, "—")),
                Metric("DESCANSO", Display(exercise.Rest, "—"))
            }
        };
        prescription.SetColumn(prescription.Children[1], 1);
        prescription.SetColumn(prescription.Children[2], 2);

        var stepList = new VerticalStackLayout { Spacing = 8 };
        for (var index = 0; index < guide.Steps.Count; index++)
        {
            var number = new Border
            {
                WidthRequest = 32,
                HeightRequest = 32,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 11 },
                BackgroundColor = Color.FromArgb("#E5F0D5"),
                Content = new Label
                {
                    Text = (index + 1).ToString(),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#284B36"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };
            var stepText = new Label
            {
                Text = guide.Steps[index],
                FontSize = 14,
                TextColor = Color.FromArgb("#33453E"),
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12,
                Children = { number, stepText }
            };
            row.SetColumn(stepText, 1);
            stepList.Children.Add(row);
        }

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 18, 20, 30),
            Spacing = 13,
            Children =
            {
                eyebrow,
                title,
                demo,
                Section("Na sua ficha"),
                prescription,
                Section("Como fazer"),
                stepList
            }
        };

        if (!string.IsNullOrWhiteSpace(exercise.Instructions))
        {
            content.Children.Add(Section("Observação"));
            content.Children.Add(InfoCard(exercise.Instructions));
        }
        if (!string.IsNullOrWhiteSpace(exercise.Priority))
        {
            content.Children.Add(Section("Prioridade"));
            content.Children.Add(InfoCard(exercise.Priority));
        }
        if (!string.IsNullOrWhiteSpace(exercise.ReferenceLoad))
        {
            content.Children.Add(Section("Referência anotada"));
            content.Children.Add(InfoCard($"{exercise.ReferenceLoad}. Esta é uma anotação da ficha, não uma carga medida."));
        }
        content.Children.Add(new Label
        {
            Text = "A animação é ilustrativa. Respeite as restrições e orientações registradas para você.",
            FontSize = 12,
            TextColor = Color.FromArgb("#71807B"),
            Margin = new Thickness(2, 6, 2, 0),
            LineBreakMode = LineBreakMode.WordWrap
        });
        Content = new ScrollView { Content = content, BackgroundColor = BackgroundColor };
    }

    private static View Metric(string label, string value) => new Border
    {
        Stroke = Color.FromArgb("#E4E9E3"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
        BackgroundColor = Colors.White,
        Padding = new Thickness(12, 14),
        Content = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                new Label { Text = label, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#71807B"), CharacterSpacing = 0.8 },
                new Label { Text = value, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#14221F"), LineBreakMode = LineBreakMode.TailTruncation }
            }
        }
    };

    private static Label Section(string text) => new()
    {
        Text = text,
        FontSize = 19,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#14221F"),
        Margin = new Thickness(1, 8, 1, 0)
    };

    private static View InfoCard(string text) => new Border
    {
        Stroke = Color.FromArgb("#E4E9E3"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
        BackgroundColor = Colors.White,
        Padding = new Thickness(15),
        Content = new Label { Text = text, FontSize = 14, TextColor = Color.FromArgb("#33453E"), LineBreakMode = LineBreakMode.WordWrap }
    };

    private static string Display(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
