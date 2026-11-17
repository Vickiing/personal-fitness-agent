using System.Globalization;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Locally cached workout viewer. Workout content is supplied by the platform sync layer, not a file picker.</summary>
public sealed class TrainingSheetPage : ContentPage
{
    private readonly Label _dayTitle = new();
    private readonly Label _daySummary = new();
    private readonly HorizontalStackLayout _daySelector = new() { Spacing = 8 };
    private readonly Button _toggleNotes = new();
    private readonly VerticalStackLayout _notesPanel = new() { Spacing = 6, IsVisible = false };
    private readonly VerticalStackLayout _notesHeader;
    private readonly CollectionView _exercises;
    private readonly Button _startSession = new()
    {
        Text = "Iniciar treino",
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#F3F6FA"),
        BackgroundColor = Color.FromArgb("#FF642D"),
        CornerRadius = 16,
        HeightRequest = 50
    };
    private WorkoutSheet? _sheet;
    private WorkoutSheetDay? _selectedDay;

    public TrainingSheetPage()
    {
        Title = "Treinos";
        BackgroundColor = Color.FromArgb("#0B0F14");

        var dayTabs = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = _daySelector
        };

        _dayTitle.FontSize = 18;
        _dayTitle.FontAttributes = FontAttributes.Bold;
        _dayTitle.TextColor = Color.FromArgb("#F3F6FA");
        _daySummary.FontSize = 13;
        _daySummary.TextColor = Color.FromArgb("#A7B2BF");
        _daySummary.LineBreakMode = LineBreakMode.WordWrap;
        _toggleNotes.FontSize = 12;
        _toggleNotes.TextColor = Color.FromArgb("#CFD8E2");
        _toggleNotes.BackgroundColor = Colors.Transparent;
        _toggleNotes.HorizontalOptions = LayoutOptions.Start;
        _toggleNotes.HeightRequest = 36;
        _toggleNotes.Padding = new Thickness(0);
        var dayInfo = new VerticalStackLayout { Spacing = 3, Children = { _dayTitle, _daySummary } };
        _notesHeader = new VerticalStackLayout
        {
            Padding = new Thickness(20, 0, 20, 8),
            Spacing = 6,
            Children = { _toggleNotes, _notesPanel }
        };
        _toggleNotes.Clicked += ToggleNotes;
        _startSession.Clicked += async (_, _) =>
        {
            if (_sheet is null || _selectedDay is null) return;
            var page = new WorkoutSessionPage(_sheet, _selectedDay);
            if (await page.StartAsync())
                await Shell.Current.Navigation.PushAsync(page);
        };
        var header = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12, 18, 8),
            Spacing = 7,
            Children = { dayTabs, dayInfo, _startSession }
        };

        _exercises = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepScrollOffset,
            EmptyView = EmptyWorkoutState(),
            Header = _notesHeader,
            ItemTemplate = new DataTemplate(CreateExerciseCard)
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            BackgroundColor = BackgroundColor,
            Children = { header, _exercises }
        };
        Grid.SetRow(_exercises, 1);
        RefreshFromStore();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LocalWorkoutSeed.LoadIfMissingAsync();
        RefreshFromStore();
    }

    private static View EmptyWorkoutState()
    {
        var content = new VerticalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "TREINOS", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF642D"), CharacterSpacing = 1.5, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Sua ficha aparecerá aqui", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA"), HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Quando um treino for disponibilizado na plataforma web, ele ficará salvo neste aparelho para consulta offline. Não é preciso importar planilha.", FontSize = 14, TextColor = Color.FromArgb("#A7B2BF"), HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center, MaximumWidthRequest = 330, LineBreakMode = LineBreakMode.WordWrap }
            }
        };
        return new Border
        {
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(24),
            Margin = new Thickness(20, 8, 20, 20),
            Content = content
        };
    }

    private View CreateExerciseCard()
    {
        var preview = new Image
        {
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        preview.SetBinding(Image.SourceProperty,
            new Binding(nameof(WorkoutSheetExercise.Name), converter: new ExerciseThumbnailConverter()));
        var videoBadge = new Label
        {
            Text = "▶ VÍDEO",
            FontSize = 8,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#D90B0F14"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(2)
        };
        videoBadge.SetBinding(IsVisibleProperty,
            new Binding(nameof(WorkoutSheetExercise.Name), converter: new ExerciseVideoVisibilityConverter()));
        var posterContent = new Grid { Children = { preview, videoBadge } };
        var poster = new Border
        {
            WidthRequest = 66,
            HeightRequest = 74,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            BackgroundColor = Color.FromArgb("#18212B"),
            VerticalOptions = LayoutOptions.Center,
            Content = posterContent
        };
        var order = new Label
        {
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#A7B2BF"),
            CharacterSpacing = 1.1
        };
        order.SetBinding(Label.TextProperty, new Binding(nameof(WorkoutSheetExercise.Order), stringFormat: "MOVIMENTO {0:00}"));
        var name = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F3F6FA"),
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 2
        };
        name.SetBinding(Label.TextProperty, nameof(WorkoutSheetExercise.Name));
        var prescription = new Label { FontSize = 12, TextColor = Color.FromArgb("#A7B2BF"), LineBreakMode = LineBreakMode.WordWrap };
        prescription.SetBinding(Label.TextProperty, new Binding(".", converter: new ExerciseCardSummaryConverter()));
        var text = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center, Children = { order, name, prescription } };
        var arrow = new Label { Text = "›", FontSize = 30, TextColor = Color.FromArgb("#8997A6"), VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center };
        var layout = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 13,
            Children = { poster, text, arrow }
        };
        layout.SetColumn(text, 1);
        layout.SetColumn(arrow, 2);
        var card = new Border
        {
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(12),
            Margin = new Thickness(20, 0, 20, 0),
            Content = layout
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (card.BindingContext is WorkoutSheetExercise exercise && _selectedDay is { } day && _sheet is { } sheet)
                {
                    var initialIndex = Math.Max(0, day.Exercises.ToList().FindIndex(item => item.Order == exercise.Order));
                    var plan = WorkoutSessionPage.CreatePlanFor(sheet, day, App.Data.Exercises);
                    var exerciseId = plan.Exercises[initialIndex].Id;
                    await Shell.Current.Navigation.PushAsync(new ExerciseDetailPage(day.Key, exercise, async () =>
                    {
                        var sessionPage = new WorkoutSessionPage(sheet, day, initialIndex);
                        if (await sessionPage.StartAsync())
                            await Shell.Current.Navigation.PushAsync(sessionPage);
                    }, exerciseId, App.Data.Sessions));
                }
            })
        });
        SemanticProperties.SetDescription(card, "Toque para abrir os detalhes e a demonstração do exercício.");
        return card;
    }

    private void RefreshFromStore()
    {
        _sheet = App.Data.ImportedWorkoutSheet;
        if (_sheet is null)
        {
            _selectedDay = null;
            _exercises.ItemsSource = null;
            _dayTitle.Text = "Aguardando sua ficha";
            _daySummary.Text = "A ficha aparecerá após ser disponibilizada pela plataforma web.";
            _startSession.IsEnabled = false;
            UpdateDayButtons();
            return;
        }
        _selectedDay = _sheet.Days.FirstOrDefault(day => day.Key == _selectedDay?.Key)
            ?? _sheet.Days.FirstOrDefault(day => day.Key == "A")
            ?? _sheet.Days[0];
        ShowSelectedDay();
    }

    private void SelectDay(string key)
    {
        if (_selectedDay?.Key == key) return;
        _selectedDay = _sheet?.Days.FirstOrDefault(day => day.Key == key);
        ShowSelectedDay();
    }

    private void ShowSelectedDay()
    {
        UpdateDayButtons();
        if (_selectedDay is null)
        {
            _exercises.ItemsSource = null;
            return;
        }
        _dayTitle.Text = $"Treino {_selectedDay.Key}";
        var plan = WorkoutSessionPage.CreatePlanFor(_sheet!, _selectedDay, App.Data.Exercises);
        var hasPendingSession = App.Data.Sessions.Any(session => session.PlanId == plan.Id && session.CompletedUtc is null);
        _startSession.Text = hasPendingSession ? "Retomar treino" : "Iniciar treino";
        _startSession.IsEnabled = true;
        _daySummary.Text = $"{_selectedDay.Exercises.Count} exercícios";
        _notesPanel.Children.Clear();
        foreach (var note in _selectedDay.Notes)
        {
            _notesPanel.Children.Add(new Label
            {
                Text = note,
                FontSize = 12,
                TextColor = Color.FromArgb("#CFD8E2"),
                BackgroundColor = Color.FromArgb("#1B2732"),
                Padding = new Thickness(12, 8),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }
        _toggleNotes.Text = _selectedDay.Notes.Count == 0 ? string.Empty : $"Orientações ({_selectedDay.Notes.Count})  ⌄";
        _toggleNotes.IsVisible = _selectedDay.Notes.Count > 0;
        _exercises.ItemsSource = _selectedDay.Exercises;
    }

    private void ToggleNotes(object? sender, EventArgs e)
    {
        _notesPanel.IsVisible = !_notesPanel.IsVisible;
        _toggleNotes.Text = _notesPanel.IsVisible
            ? $"Orientações ({_notesPanel.Children.Count})  ⌃"
            : $"Orientações ({_notesPanel.Children.Count})  ⌄";
    }

    private void UpdateDayButtons()
    {
        if (_sheet is null)
        {
            _daySelector.Children.Clear();
            return;
        }

        var labels = _sheet.Days.Select(day => $"Treino {day.Key}").ToArray();
        var existingButtons = _daySelector.Children.OfType<Button>().ToArray();
        if (!existingButtons.Select(button => button.Text).SequenceEqual(labels, StringComparer.Ordinal))
        {
            _daySelector.Children.Clear();
            foreach (var day in _sheet.Days)
            {
                var button = new Button();
                StyleDayButton(button, $"Treino {day.Key}");
                button.Clicked += (_, _) => SelectDay(day.Key);
                SemanticProperties.SetDescription(button, $"Mostrar todos os exercícios do treino {day.Key}.");
                _daySelector.Children.Add(button);
            }
        }

        foreach (var button in _daySelector.Children.OfType<Button>())
            SetDayStyle(button, button.Text == $"Treino {_selectedDay?.Key}");
    }

    private static void StyleDayButton(Button button, string text)
    {
        button.Text = text;
        button.FontSize = 12;
        button.FontAttributes = FontAttributes.Bold;
        button.CornerRadius = 16;
        button.HeightRequest = 44;
        button.Padding = new Thickness(12, 0);
        button.BorderWidth = 0;
    }

    private static void SetDayStyle(Button button, bool selected)
    {
        button.BackgroundColor = Color.FromArgb(selected ? "#18212B" : "#1B2732");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#CFD8E2");
    }
}

internal sealed class ExerciseThumbnailConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name)) return null!;
        var url = ExerciseDemoView.PreviewUrlFor(ExerciseGuideCatalog.For(name).Motion);
        return url is null ? null! : ImageSource.FromUri(new Uri(url));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class ExerciseVideoVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string name && !string.IsNullOrWhiteSpace(name) &&
        ExerciseDemoView.HasVideoFor(ExerciseGuideCatalog.For(name).Motion);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class ExerciseCardSummaryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WorkoutSheetExercise exercise) return "";
        var prescription = string.Join("   ·   ", new[]
        {
            string.IsNullOrWhiteSpace(exercise.Sets) ? null : $"{exercise.Sets} séries",
            string.IsNullOrWhiteSpace(exercise.Repetitions) ? null : $"{exercise.Repetitions} reps",
            string.IsNullOrWhiteSpace(exercise.Rest) ? null : $"{exercise.Rest} descanso"
        }.Where(part => part is not null));
        return string.IsNullOrWhiteSpace(exercise.Priority) ? prescription : $"{prescription}   ·   {exercise.Priority}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class ExercisePosterDrawable(ExerciseMotion motion) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var center = dirtyRect.Width / 2;
        var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / 84f;
        float X(float value) => center + value * scale;
        float Y(float value) => 3 + value * scale;
        canvas.SaveState();
        canvas.FillColor = Color.FromArgb("#1A2631");
        canvas.FillRoundedRectangle(dirtyRect, 16);
        canvas.StrokeColor = Color.FromArgb("#8997A6");
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = 3 * scale;
        switch (motion)
        {
            case ExerciseMotion.LatPulldown:
                canvas.DrawLine(X(-27), Y(7), X(27), Y(7));
                canvas.DrawLine(X(-27), Y(7), X(-27), Y(69));
                canvas.DrawLine(X(27), Y(7), X(27), Y(69));
                canvas.DrawLine(X(-25), Y(10), X(-18), Y(10));
                canvas.DrawLine(X(25), Y(10), X(18), Y(10));
                break;
            case ExerciseMotion.CableRow:
            case ExerciseMotion.TricepsPushdown:
                canvas.DrawLine(X(28), Y(8), X(28), Y(69));
                canvas.DrawLine(X(23), Y(8), X(33), Y(8));
                canvas.DrawLine(X(28), Y(30), X(19), Y(35));
                break;
            case ExerciseMotion.SupportedRow:
            case ExerciseMotion.ChestFly:
            case ExerciseMotion.ReverseFly:
                canvas.DrawLine(X(-16), Y(36), X(16), Y(36));
                canvas.DrawLine(X(-16), Y(36), X(-16), Y(53));
                canvas.DrawLine(X(16), Y(36), X(16), Y(53));
                break;
            case ExerciseMotion.LegExtension:
            case ExerciseMotion.LegCurl:
                canvas.DrawLine(X(-12), Y(50), X(18), Y(50));
                canvas.DrawLine(X(18), Y(50), X(23), Y(67));
                canvas.DrawLine(X(-18), Y(49), X(-18), Y(66));
                break;
        }

        var seated = motion is ExerciseMotion.LegExtension or ExerciseMotion.LegCurl or ExerciseMotion.SeatedCurl;
        var hipY = seated ? 47f : 44f;
        var shoulderY = seated ? 24f : 22f;
        var (leftElbowX, leftElbowY, leftHandX, leftHandY, rightElbowX, rightElbowY, rightHandX, rightHandY) = motion switch
        {
            ExerciseMotion.LatPulldown => (-19f, 17f, -11f, 29f, 19f, 17f, 11f, 29f),
            ExerciseMotion.LateralRaise => (-21f, 28f, -31f, 17f, 21f, 28f, 31f, 17f),
            ExerciseMotion.ChestFly => (-23f, 25f, -14f, 31f, 23f, 25f, 14f, 31f),
            ExerciseMotion.ReverseFly => (-22f, 27f, -32f, 24f, 22f, 27f, 32f, 24f),
            ExerciseMotion.TricepsPushdown => (-8f, 32f, -8f, 45f, 8f, 32f, 19f, 38f),
            ExerciseMotion.SeatedCurl => (-11f, 36f, -9f, 28f, 11f, 36f, 9f, 28f),
            ExerciseMotion.SupportedRow or ExerciseMotion.CableRow => (-17f, 33f, -11f, 34f, 17f, 33f, 11f, 34f),
            _ => (-17f, 30f, -20f, 37f, 17f, 30f, 20f, 37f)
        };

        canvas.FillColor = Color.FromArgb("#FF642D");
        canvas.FillCircle(X(0), Y(12), 5.5f * scale);
        canvas.StrokeColor = Color.FromArgb("#18212B");
        canvas.StrokeSize = 5 * scale;
        canvas.DrawLine(X(0), Y(18), X(0), Y(hipY));
        canvas.DrawLine(X(0), Y(shoulderY), X(leftElbowX), Y(leftElbowY));
        canvas.DrawLine(X(leftElbowX), Y(leftElbowY), X(leftHandX), Y(leftHandY));
        canvas.DrawLine(X(0), Y(shoulderY), X(rightElbowX), Y(rightElbowY));
        canvas.DrawLine(X(rightElbowX), Y(rightElbowY), X(rightHandX), Y(rightHandY));

        if (motion is ExerciseMotion.LegExtension or ExerciseMotion.LegCurl)
        {
            canvas.DrawLine(X(0), Y(hipY), X(-11), Y(54));
            canvas.DrawLine(X(-11), Y(54), X(-23), Y(66));
            canvas.DrawLine(X(0), Y(hipY), X(11), Y(54));
            var footY = motion == ExerciseMotion.LegExtension ? 61 : 66;
            canvas.DrawLine(X(11), Y(54), X(25), Y(footY));
        }
        else if (motion == ExerciseMotion.BodyweightLunge)
        {
            canvas.DrawLine(X(0), Y(hipY), X(-15), Y(53));
            canvas.DrawLine(X(-15), Y(53), X(-19), Y(68));
            canvas.DrawLine(X(0), Y(hipY), X(17), Y(55));
            canvas.DrawLine(X(17), Y(55), X(29), Y(67));
        }
        else if (motion == ExerciseMotion.BodyweightSquat)
        {
            canvas.DrawLine(X(0), Y(hipY), X(-17), Y(53));
            canvas.DrawLine(X(-17), Y(53), X(-17), Y(68));
            canvas.DrawLine(X(0), Y(hipY), X(17), Y(53));
            canvas.DrawLine(X(17), Y(53), X(17), Y(68));
        }
        else if (seated)
        {
            canvas.DrawLine(X(0), Y(hipY), X(-10), Y(55));
            canvas.DrawLine(X(-10), Y(55), X(16), Y(55));
            canvas.DrawLine(X(0), Y(hipY), X(10), Y(55));
            canvas.DrawLine(X(10), Y(55), X(25), Y(55));
        }
        else
        {
            canvas.DrawLine(X(0), Y(hipY), X(-10), Y(58));
            canvas.DrawLine(X(-10), Y(58), X(-15), Y(68));
            canvas.DrawLine(X(0), Y(hipY), X(10), Y(58));
            canvas.DrawLine(X(10), Y(58), X(15), Y(68));
        }

        canvas.StrokeColor = Color.FromArgb("#8997A6");
        canvas.StrokeSize = 2 * scale;
        canvas.DrawLine(X(-32), Y(71), X(32), Y(71));
        canvas.RestoreState();
    }
}
