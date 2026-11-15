using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public sealed class ExerciseDetailPage : ContentPage
{
    public ExerciseDetailPage(string dayKey, WorkoutSheetExercise exercise, Func<Task>? startSession = null,
        Guid? exerciseId = null, IReadOnlyList<SessionDto>? sessions = null)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        var guide = ExerciseGuideCatalog.For(exercise.Name);
        Title = "Movimento";
        BackgroundColor = Color.FromArgb("#0B0F14");

        var eyebrow = new Label
        {
            Text = string.IsNullOrWhiteSpace(dayKey) ? "DEMONSTRAÇÃO" : $"TREINO {dayKey.ToUpperInvariant()}  ·  MOVIMENTO {exercise.Order:00}",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#A7B2BF"),
            CharacterSpacing = 1.5
        };
        var title = new Label
        {
            Text = exercise.Name,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F3F6FA"),
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
                BackgroundColor = Color.FromArgb("#35241D"),
                Content = new Label
                {
                    Text = (index + 1).ToString(),
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FF9A6B"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };
            var stepText = new Label
            {
                Text = guide.Steps[index],
                FontSize = 14,
                TextColor = Color.FromArgb("#CFD8E2"),
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
                Section("Séries e descanso"),
                prescription,
                Section("Como fazer"),
                stepList
            }
        };

        content.Children.Add(Section("Suas cargas recentes"));
        content.Children.Add(RecentSets(exerciseId, sessions));

        if (startSession is not null)
        {
            var registerSet = new Button
            {
                Text = "Registrar série neste treino",
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 50,
                CornerRadius = 16,
                BackgroundColor = Color.FromArgb("#18212B"),
                TextColor = Colors.White
            };
            registerSet.Clicked += async (_, _) => await startSession();
            content.Children.Insert(3, registerSet);
        }

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
        Content = new ScrollView { Content = content, BackgroundColor = BackgroundColor };
    }

    private static View RecentSets(Guid? exerciseId, IReadOnlyList<SessionDto>? sessions)
    {
        var latest = exerciseId is null ? null : sessions?
            .Where(session => session.Sets.Any(set => set.ExerciseId == exerciseId.Value))
            .OrderByDescending(session => session.StartedUtc)
            .FirstOrDefault();
        if (latest is null)
            return InfoCard("Você ainda não registrou séries neste exercício.");

        var rows = new VerticalStackLayout { Spacing = 6 };
        rows.Children.Add(new Label
        {
            Text = latest.StartedAtLocal.ToString("dd/MM/yyyy"),
            FontSize = 12,
            TextColor = Color.FromArgb("#A7B2BF")
        });
        foreach (var set in latest.Sets.Where(set => set.ExerciseId == exerciseId).OrderBy(set => set.Index))
        {
            var load = set.LoadKg is null ? "Carga não informada" : $"{set.LoadKg.Value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)} kg";
            var reps = set.Reps is null ? "repetições não informadas" : $"{set.Reps} repetições";
            rows.Children.Add(new Label
            {
                Text = $"Série {set.Index}  ·  {load}  ·  {reps}",
                FontSize = 14,
                TextColor = Color.FromArgb("#CFD8E2"),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }
        return new Border
        {
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(15),
            Content = rows
        };
    }

    private static View Metric(string label, string value) => new Border
    {
        Stroke = Color.FromArgb("#293440"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
        BackgroundColor = Color.FromArgb("#151C24"),
        Padding = new Thickness(12, 14),
        Content = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                new Label { Text = label, FontSize = 9, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#A7B2BF"), CharacterSpacing = 0.8 },
                new Label { Text = value, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), LineBreakMode = LineBreakMode.TailTruncation }
            }
        }
    };

    private static Label Section(string text) => new()
    {
        Text = text,
        FontSize = 19,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#F3F6FA"),
        Margin = new Thickness(1, 8, 1, 0)
    };

    private static View InfoCard(string text) => new Border
    {
        Stroke = Color.FromArgb("#293440"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
        BackgroundColor = Color.FromArgb("#151C24"),
        Padding = new Thickness(15),
        Content = new Label { Text = text, FontSize = 14, TextColor = Color.FromArgb("#CFD8E2"), LineBreakMode = LineBreakMode.WordWrap }
    };

    private static string Display(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
