using System.Globalization;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#14221F");
    private static readonly Color Muted = Color.FromArgb("#71807B");
    private readonly Label _workoutLabel = new();
    private readonly Label _summaryLabel = new();

    public MainPage()
    {
        Title = "Hoje";
        BackgroundColor = Color.FromArgb("#F4F6F2");

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var date = new Label
        {
            Text = DateTime.Now.ToString("dddd, d 'de' MMMM", culture),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#63736D")
        };
        var greeting = new Label
        {
            Text = "Seu próximo treino começa aqui.",
            FontSize = 29,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ink,
            LineBreakMode = LineBreakMode.WordWrap
        };
        SemanticProperties.SetHeadingLevel(greeting, SemanticHeadingLevel.Level1);

        var heroTitle = new Label
        {
            Text = "MINHA FICHA",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#BFE46B"),
            CharacterSpacing = 1.8
        };
        _workoutLabel.TextColor = Colors.White;
        _workoutLabel.FontSize = 24;
        _workoutLabel.FontAttributes = FontAttributes.Bold;
        _workoutLabel.LineBreakMode = LineBreakMode.WordWrap;
        var heroSubtitle = new Label
        {
            Text = "Treinos e demonstrações ficam disponíveis no aparelho, mesmo sem conexão.",
            FontSize = 14,
            TextColor = Color.FromArgb("#D3DFD8"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        var openWorkout = new Button
        {
            Text = "Ver meus treinos  ›",
            BackgroundColor = Color.FromArgb("#C8F078"),
            TextColor = Color.FromArgb("#14221F"),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16,
            HeightRequest = 52,
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Thickness(18, 0)
        };
        openWorkout.Clicked += async (_, _) => await Shell.Current.GoToAsync("//workout");

        var planBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#29433B"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14, 12),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = "A / B",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#C8F078")
            }
        };

        var heroContent = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            RowSpacing = 13
        };
        heroContent.Add(heroTitle, 0, 0);
        heroContent.Add(planBadge, 1, 0);
        heroContent.Add(_workoutLabel, 0, 1);
        heroContent.SetColumnSpan(_workoutLabel, 2);
        heroContent.Add(heroSubtitle, 0, 2);
        heroContent.SetColumnSpan(heroSubtitle, 2);
        heroContent.Add(openWorkout, 0, 3);
        heroContent.SetColumnSpan(openWorkout, 2);

        var hero = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
            Background = new LinearGradientBrush(
                [new GradientStop(Color.FromArgb("#153D34"), 0f), new GradientStop(Color.FromArgb("#0B2420"), 1f)],
                new Point(0, 0), new Point(1, 1)),
            Padding = new Thickness(22),
            Content = heroContent
        };

        var weekHeading = SectionHeading("Sua semana", "RESUMO");
        var weekCard = new Border
        {
            Stroke = Color.FromArgb("#E4E9E3"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(18),
            Content = _summaryLabel
        };

        var shortcutsHeading = SectionHeading("Acesso rápido", "ATALHOS");
        var shortcuts = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12
        };
        shortcuts.Add(QuickAction("Check-in", "Sono, alimentação e sintomas", async () => await Shell.Current.GoToAsync("//checkin")), 0, 0);
        shortcuts.Add(QuickAction("Restrições", "Confira suas políticas", async () => await Shell.Current.Navigation.PushAsync(new RestrictionsPage())), 1, 0);

        var connectionNote = new Label
        {
            Text = "A ficha será sincronizada pela plataforma web quando essa conexão estiver disponível. Seus registros locais continuam funcionando offline.",
            FontSize = 12,
            TextColor = Muted,
            LineBreakMode = LineBreakMode.WordWrap,
            Margin = new Thickness(2, 4, 2, 16)
        };

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 22, 20, 24),
            Spacing = 15,
            Children = { date, greeting, hero, weekHeading, weekCard, shortcutsHeading, shortcuts, connectionNote }
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

    private void RefreshSummary()
    {
        var sheet = App.Data.ImportedWorkoutSheet;
        if (sheet is null)
        {
            _workoutLabel.Text = "Sua ficha aparece aqui";
        }
        else
        {
            var firstDay = sheet.Days.FirstOrDefault(day => day.Key == "A") ?? sheet.Days[0];
            _workoutLabel.Text = firstDay.Title;
        }

        try
        {
            var summary = App.Data.WeeklyReview(DateOnly.FromDateTime(DateTime.Now));
            var adherence = summary.AdherencePercent is null ? "—" : $"{summary.AdherencePercent:0}%";
            _summaryLabel.Text = $"{summary.WorkoutCount} treinos concluídos  ·  Adesão {adherence}\n" +
                $"Sono mediano  {summary.MedianSleepHours?.ToString("0.0", CultureInfo.CurrentCulture) + " h" ?? "sem dados"}   ·   " +
                $"Sintomas  {summary.SymptomCount}";
            _summaryLabel.FontSize = 14;
            _summaryLabel.TextColor = Ink;
            _summaryLabel.LineBreakMode = LineBreakMode.WordWrap;
        }
        catch
        {
            _summaryLabel.Text = "Seu resumo semanal aparecerá aqui.";
            _summaryLabel.FontSize = 14;
            _summaryLabel.TextColor = Ink;
        }
    }

    private static View SectionHeading(string title, string eyebrow)
    {
        var stack = new VerticalStackLayout { Spacing = 3 };
        stack.Children.Add(new Label { Text = eyebrow, FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#73847C"), CharacterSpacing = 1.5 });
        stack.Children.Add(new Label { Text = title, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Ink });
        return stack;
    }

    private static View QuickAction(string title, string subtitle, Func<Task> action)
    {
        var button = new Button
        {
            Text = $"{title}  ›\n{subtitle}",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Ink,
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E4E9E3"),
            BorderWidth = 1,
            CornerRadius = 18,
            HeightRequest = 82,
            Padding = new Thickness(14, 10),
            HorizontalOptions = LayoutOptions.Fill
        };
        AutomationProperties.SetHelpText(button, subtitle);
        button.Clicked += async (_, _) => await action();
        return button;
    }
}
