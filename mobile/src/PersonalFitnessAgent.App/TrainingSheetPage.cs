using System.Globalization;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Locally cached workout viewer. Workout content is supplied by the platform sync layer, not a file picker.</summary>
public sealed class TrainingSheetPage : ContentPage
{
    private readonly Label _dayTitle = new();
    private readonly Label _daySummary = new();
    private readonly Button _dayA = new();
    private readonly Button _dayB = new();
    private readonly CollectionView _exercises;
    private WorkoutSheet? _sheet;
    private WorkoutSheetDay? _selectedDay;

    public TrainingSheetPage()
    {
        Title = "Treinos";
        BackgroundColor = Color.FromArgb("#F4F6F2");

        var eyebrow = new Label
        {
            Text = "SUA BIBLIOTECA",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6F8178"),
            CharacterSpacing = 1.7
        };
        var title = new Label
        {
            Text = "Meus treinos",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#14221F")
        };
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
        var subtitle = new Label
        {
            Text = "Abra um movimento para assistir à demonstração e ver a ficha.",
            FontSize = 14,
            TextColor = Color.FromArgb("#71807B"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        StyleDayButton(_dayA, "TREINO A");
        StyleDayButton(_dayB, "TREINO B");
        _dayA.Clicked += (_, _) => SelectDay("A");
        _dayB.Clicked += (_, _) => SelectDay("B");
        var dayTabs = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { _dayA, _dayB }
        };
        dayTabs.SetColumn(_dayB, 1);

        _dayTitle.FontSize = 18;
        _dayTitle.FontAttributes = FontAttributes.Bold;
        _dayTitle.TextColor = Color.FromArgb("#14221F");
        _daySummary.FontSize = 13;
        _daySummary.TextColor = Color.FromArgb("#71807B");
        _daySummary.LineBreakMode = LineBreakMode.WordWrap;
        var dayInfo = new VerticalStackLayout { Spacing = 3, Children = { _dayTitle, _daySummary } };
        var header = new VerticalStackLayout
        {
            Padding = new Thickness(20, 20, 20, 10),
            Spacing = 10,
            Children = { eyebrow, title, subtitle, dayTabs, dayInfo }
        };

        _exercises = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepScrollOffset,
            EmptyView = EmptyWorkoutState(),
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
                new Label { Text = "A / B", FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1A5948"), HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Sua ficha aparecerá aqui", FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#14221F"), HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "Quando um treino for disponibilizado na plataforma web, ele ficará salvo neste aparelho para consulta offline. Não é preciso importar planilha.", FontSize = 14, TextColor = Color.FromArgb("#71807B"), HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center, MaximumWidthRequest = 330, LineBreakMode = LineBreakMode.WordWrap }
            }
        };
        return new Border
        {
            Stroke = Color.FromArgb("#E1E8E1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(24),
            Margin = new Thickness(20, 8, 20, 20),
            Content = content
        };
    }

    private View CreateExerciseCard()
    {
        var poster = new GraphicsView
        {
            Drawable = new ExercisePosterDrawable(ExerciseMotion.SupportedRow),
            WidthRequest = 66,
            HeightRequest = 74,
            BackgroundColor = Color.FromArgb("#E8F1E8"),
            VerticalOptions = LayoutOptions.Center
        };
        poster.BindingContextChanged += (_, _) =>
        {
            if (poster.BindingContext is WorkoutSheetExercise exercise)
                poster.Drawable = new ExercisePosterDrawable(ExerciseGuideCatalog.For(exercise.Name).Motion);
        };
        var order = new Label
        {
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#71807B"),
            CharacterSpacing = 1.1
        };
        order.SetBinding(Label.TextProperty, new Binding(nameof(WorkoutSheetExercise.Order), stringFormat: "MOVIMENTO {0:00}"));
        var name = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#14221F"),
            LineBreakMode = LineBreakMode.WordWrap,
            MaxLines = 2
        };
        name.SetBinding(Label.TextProperty, nameof(WorkoutSheetExercise.Name));
        var prescription = new Label { FontSize = 12, TextColor = Color.FromArgb("#63736D"), LineBreakMode = LineBreakMode.WordWrap };
        prescription.SetBinding(Label.TextProperty, new Binding(".", converter: new ExerciseCardSummaryConverter()));
        var text = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center, Children = { order, name, prescription } };
        var arrow = new Label { Text = "›", FontSize = 30, TextColor = Color.FromArgb("#8A9A91"), VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center };
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
            Stroke = Color.FromArgb("#E4E9E3"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(12),
            Margin = new Thickness(20, 0, 20, 0),
            Content = layout
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (card.BindingContext is WorkoutSheetExercise exercise)
                    await Shell.Current.Navigation.PushAsync(new ExerciseDetailPage(_selectedDay?.Key ?? "", exercise));
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
        _dayTitle.Text = _selectedDay.Title;
        _daySummary.Text = string.IsNullOrWhiteSpace(_selectedDay.Summary)
            ? $"{_selectedDay.Exercises.Count} movimentos"
            : _selectedDay.Summary;
        var notes = _selectedDay.Notes.Select(note => new Label
        {
            Text = note,
            FontSize = 12,
            TextColor = Color.FromArgb("#43564E"),
            BackgroundColor = Color.FromArgb("#E8F0E7"),
            Padding = new Thickness(12, 8),
            LineBreakMode = LineBreakMode.WordWrap
        }).ToArray();
        if (notes.Length == 0)
        {
            _exercises.Header = null;
        }
        else
        {
            var noteHeader = new VerticalStackLayout { Padding = new Thickness(20, 0, 20, 8), Spacing = 6 };
            foreach (var note in notes) noteHeader.Children.Add(note);
            _exercises.Header = noteHeader;
        }
        _exercises.ItemsSource = _selectedDay.Exercises;
    }

    private void UpdateDayButtons()
    {
        SetDayStyle(_dayA, _selectedDay?.Key == "A");
        SetDayStyle(_dayB, _selectedDay?.Key == "B");
        _dayA.IsEnabled = _sheet?.Days.Any(day => day.Key == "A") == true;
        _dayB.IsEnabled = _sheet?.Days.Any(day => day.Key == "B") == true;
        _dayA.Opacity = _dayA.IsEnabled ? 1 : 0.55;
        _dayB.Opacity = _dayB.IsEnabled ? 1 : 0.55;
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
        button.BackgroundColor = Color.FromArgb(selected ? "#153D34" : "#E8EDE7");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#42574E");
    }
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
        canvas.FillColor = Color.FromArgb("#E8F1E8");
        canvas.FillRoundedRectangle(dirtyRect, 16);
        canvas.StrokeColor = Color.FromArgb("#7E9A87");
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

        canvas.FillColor = Color.FromArgb("#C8F078");
        canvas.FillCircle(X(0), Y(12), 5.5f * scale);
        canvas.StrokeColor = Color.FromArgb("#183F34");
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

        canvas.StrokeColor = Color.FromArgb("#91A697");
        canvas.StrokeSize = 2 * scale;
        canvas.DrawLine(X(-32), Y(71), X(32), Y(71));
        canvas.RestoreState();
    }
}
