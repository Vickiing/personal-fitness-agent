using System.Globalization;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#F3F6FA");
    private static readonly Color Muted = Color.FromArgb("#A7B2BF");
    private readonly Label _workoutLabel = new();
    private readonly Label _activityLabel = new();
    private readonly Label _workoutsMetric = new();
    private readonly Label _adherenceMetric = new();
    private readonly Label _sleepMetric = new();
    private readonly Label _symptomsMetric = new();
    private readonly Button _openWorkout = new();

    public MainPage()
    {
        Title = "Hoje";
        BackgroundColor = Color.FromArgb("#0B0F14");

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var date = new Label
        {
            Text = DateTime.Now.ToString("dddd, d 'de' MMMM", culture),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Muted
        };
        var greeting = new Label
        {
            Text = DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite",
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ink
        };
        SemanticProperties.SetHeadingLevel(greeting, SemanticHeadingLevel.Level1);

        var heroCaption = new Label
        {
            Text = "SEU TREINO",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FF642D"),
            CharacterSpacing = 1.6
        };
        _workoutLabel.TextColor = Colors.White;
        _workoutLabel.FontSize = 21;
        _workoutLabel.FontAttributes = FontAttributes.Bold;
        _workoutLabel.LineBreakMode = LineBreakMode.WordWrap;
        _openWorkout.Text = "Ver treinos  ›";
        _openWorkout.BackgroundColor = Color.FromArgb("#FF642D");
        _openWorkout.TextColor = Ink;
        _openWorkout.FontSize = 14;
        _openWorkout.FontAttributes = FontAttributes.Bold;
        _openWorkout.CornerRadius = 15;
        _openWorkout.HeightRequest = 48;
        _openWorkout.HorizontalOptions = LayoutOptions.Start;
        _openWorkout.Padding = new Thickness(16, 0);
        _openWorkout.Clicked += async (_, _) => await OpenWorkoutAsync();

        var hero = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 26 },
            Background = new LinearGradientBrush(
                [new GradientStop(Color.FromArgb("#24364A"), 0f), new GradientStop(Color.FromArgb("#0B0F14"), 1f)],
                new Point(0, 0), new Point(1, 1)),
            Padding = new Thickness(20),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { heroCaption, _workoutLabel, _openWorkout }
            }
        };

        var metricsHeading = SectionHeading("Esta semana");
        var metrics = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            RowSpacing = 12,
            Children =
            {
                MetricCard("TREINOS", _workoutsMetric, "#1A2631"),
                MetricCard("ADESÃO", _adherenceMetric, "#202832"),
                MetricCard("SONO MÉDIO", _sleepMetric, "#1D2934"),
                MetricCard("SINTOMAS", _symptomsMetric, "#2B211D")
            }
        };
        metrics.SetColumn(metrics.Children[1], 1);
        metrics.SetRow(metrics.Children[2], 1);
        metrics.SetColumn(metrics.Children[2], 0);
        metrics.SetRow(metrics.Children[3], 1);
        metrics.SetColumn(metrics.Children[3], 1);

        _activityLabel.FontSize = 13;
        _activityLabel.TextColor = Muted;
        _activityLabel.LineBreakMode = LineBreakMode.WordWrap;

        var accountButton = new Button
        {
            Text = "Minha conta  ›",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ink,
            BackgroundColor = Color.FromArgb("#151C24"),
            BorderColor = Color.FromArgb("#293440"),
            BorderWidth = 1,
            CornerRadius = 16,
            HeightRequest = 48
        };
        accountButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("//account");

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 18, 20, 24),
            Spacing = 14,
            Children = { date, greeting, hero, metricsHeading, metrics, _activityLabel, accountButton }
        };
        Content = new ScrollView { Content = content, BackgroundColor = BackgroundColor };
        RefreshSummary();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalWorkoutSeed.LoadIfMissingAsync();
        RefreshSummary();
    }

    private async Task OpenWorkoutAsync()
    {
        var sheet = App.Data.ImportedWorkoutSheet;
        var pending = App.Data.Sessions
            .Where(session => session.CompletedUtc is null)
            .OrderByDescending(session => session.StartedUtc)
            .FirstOrDefault();

        if (sheet is not null && pending is not null)
        {
            var day = sheet.Days.FirstOrDefault(candidate =>
                WorkoutSessionPage.CreatePlanFor(sheet, candidate, App.Data.Exercises).Id == pending.PlanId);
            if (day is not null)
            {
                var page = new WorkoutSessionPage(sheet, day);
                if (await page.StartAsync())
                    await Shell.Current.Navigation.PushAsync(page);
                return;
            }
        }

        await Shell.Current.GoToAsync("//workout");
    }

    private void RefreshSummary()
    {
        var sheet = App.Data.ImportedWorkoutSheet;
        var firstDay = sheet?.Days.FirstOrDefault(day => day.Key == "A") ?? sheet?.Days.FirstOrDefault();
        _workoutLabel.Text = firstDay is null ? "Treino não disponível" : $"Treino {firstDay.Key}  ·  {firstDay.Exercises.Count} exercícios";

        var sessions = App.Data.Sessions.OrderByDescending(session => session.StartedUtc).ToArray();
        var inProgress = sessions.FirstOrDefault(session => session.CompletedUtc is null);
        if (inProgress is not null)
        {
            var activeDay = sheet?.Days.FirstOrDefault(day =>
                WorkoutSessionPage.CreatePlanFor(sheet, day, App.Data.Exercises).Id == inProgress.PlanId);
            if (activeDay is not null) _workoutLabel.Text = $"Treino {activeDay.Key}  ·  em andamento";
            _openWorkout.Text = "Retomar treino  ›";
            _activityLabel.Text = $"Sessão iniciada às {inProgress.StartedAtLocal:HH:mm}";
        }
        else
        {
            _openWorkout.Text = "Ver treinos  ›";
            var lastCompleted = sessions.FirstOrDefault(session => session.CompletedUtc is not null);
            _activityLabel.Text = lastCompleted is null
                ? "Ainda sem treinos registrados"
                : $"Último treino · {lastCompleted.StartedAtLocal:dd/MM}";
        }

        try
        {
            var summary = App.Data.WeeklyReview(DateOnly.FromDateTime(DateTime.Now));
            _workoutsMetric.Text = summary.WorkoutCount.ToString(CultureInfo.CurrentCulture);
            _adherenceMetric.Text = summary.AdherencePercent is null ? "—" : $"{summary.AdherencePercent:0}%";
            _sleepMetric.Text = summary.MedianSleepHours is { } hours
                ? $"{hours.ToString("0.0", CultureInfo.CurrentCulture)} h"
                : "—";
            _symptomsMetric.Text = summary.SymptomCount.ToString(CultureInfo.CurrentCulture);
        }
        catch
        {
            _workoutsMetric.Text = "—";
            _adherenceMetric.Text = "—";
            _sleepMetric.Text = "—";
            _symptomsMetric.Text = "—";
        }
    }

    private static View SectionHeading(string title) => new Label
    {
        Text = title,
        FontSize = 20,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ink,
        Margin = new Thickness(0, 4, 0, 0)
    };

    private static View MetricCard(string title, Label value, string background)
    {
        value.FontSize = 24;
        value.FontAttributes = FontAttributes.Bold;
        value.TextColor = Ink;
        var caption = new Label
        {
            Text = title,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1.1,
            TextColor = Muted
        };
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb(background),
            Padding = new Thickness(15, 12),
            MinimumHeightRequest = 88,
            Content = new VerticalStackLayout { Spacing = 5, Children = { caption, value } }
        };
    }
}
