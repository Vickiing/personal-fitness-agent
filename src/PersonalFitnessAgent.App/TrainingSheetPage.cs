using System.Globalization;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Application;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

public sealed class TrainingSheetPage : ContentPage
{
    private static readonly FilePickerFileType XlsxFile = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [DevicePlatform.iOS] = ["org.openxmlformats.spreadsheetml.sheet"],
        [DevicePlatform.MacCatalyst] = ["org.openxmlformats.spreadsheetml.sheet"],
        [DevicePlatform.WinUI] = [".xlsx"]
    });

    private readonly Label _title = Ui.Heading("Minha ficha A/B");
    private readonly Label _summary = Ui.Body("Importe sua planilha para visualizar os treinos.");
    private readonly Label _status = Ui.Body("");
    private readonly Button _dayA = Ui.Button("Treino A", (_, _) => { });
    private readonly Button _dayB = Ui.Button("Treino B", (_, _) => { });
    private readonly Button _import = Ui.Button("Importar planilha XLSX", (_, _) => { });
    private readonly CollectionView _exercises;
    private WorkoutSheet? _sheet;
    private WorkoutSheetDay? _selectedDay;

    public TrainingSheetPage()
    {
        Title = "Minha ficha";
        _dayA.Clicked += (_, _) => SelectDay("A");
        _dayB.Clicked += (_, _) => SelectDay("B");
        _import.Clicked += async (_, _) => await ImportWorkbook();
        _exercises = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 10 },
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            EmptyView = new Label
            {
                Text = "Importe a planilha XLSX para ver os exercícios desta sessão.",
                Padding = 24,
                TextColor = Color.FromArgb("#64716E"),
                HorizontalTextAlignment = TextAlignment.Center
            },
            ItemTemplate = new DataTemplate(() => CreateExerciseCard())
        };

        var dayButtons = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 8 };
        dayButtons.Add(_dayA, 0, 0);
        dayButtons.Add(_dayB, 1, 0);

        var header = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16, 20, 8),
            Spacing = 10,
            Children =
            {
                _title,
                Ui.Body("Visualização local da sua ficha. Toque em um exercício para ver a demonstração e o passo a passo."),
                dayButtons,
                _summary,
                _status
            }
        };
        var footer = new VerticalStackLayout
        {
            Padding = new Thickness(20, 8, 20, 16),
            Children = { _import }
        };
        var layout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            BackgroundColor = Color.FromArgb("#F5F4EF")
        };
        layout.Add(header, 0, 0);
        layout.Add(_exercises, 0, 1);
        layout.Add(footer, 0, 2);
        Content = layout;
        RefreshFromStore();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshFromStore();
    }

    private View CreateExerciseCard()
    {
        var order = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#145A50") };
        order.SetBinding(Label.TextProperty, new Binding(nameof(WorkoutSheetExercise.Order), stringFormat: "{0:00}"));
        var name = new Label { FontSize = 17, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#202A29"), LineBreakMode = LineBreakMode.WordWrap };
        name.SetBinding(Label.TextProperty, nameof(WorkoutSheetExercise.Name));
        var details = new Label { FontSize = 14, TextColor = Color.FromArgb("#64716E") };
        details.SetBinding(Label.TextProperty, new Binding(".", converter: new ExerciseCardSummaryConverter()));
        var open = Ui.Button("Detalhes do exercício", async (sender, _) =>
        {
            if ((sender as Button)?.BindingContext is WorkoutSheetExercise exercise)
                await Shell.Current.Navigation.PushAsync(new ExerciseDetailPage(_selectedDay?.Key ?? "", exercise));
        }, secondary: true);
        open.SetBinding(BindingContextProperty, ".");
        var card = new Border
        {
            Stroke = Color.FromArgb("#E1E6E1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Colors.White,
            Padding = 14,
            Margin = new Thickness(16, 0),
            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Children = { order, name, details, open }
            }
        };
        SemanticProperties.SetDescription(card, "Cartão de exercício. Abra os detalhes para ver a execução.");
        return card;
    }

    private void RefreshFromStore()
    {
        _sheet = App.Data.ImportedWorkoutSheet;
        if (_sheet is null)
        {
            _selectedDay = null;
            _exercises.ItemsSource = null;
            _summary.Text = "Sua ficha ainda não foi importada. O arquivo será lido localmente e guardado neste aparelho.";
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
        _summary.Text = $"{_selectedDay.Title}\n{_selectedDay.Summary}";
        var header = new VerticalStackLayout { Padding = new Thickness(20, 4), Spacing = 6 };
        foreach (var note in _selectedDay.Notes)
            header.Children.Add(new Label
            {
                Text = note,
                FontSize = 13,
                TextColor = Color.FromArgb("#465450"),
                BackgroundColor = Color.FromArgb("#EAF0EC"),
                Padding = 10
            });
        _exercises.Header = header.Children.Count == 0 ? null : header;
        _exercises.ItemsSource = _selectedDay.Exercises;
    }

    private void UpdateDayButtons()
    {
        SetDayStyle(_dayA, _selectedDay?.Key == "A");
        SetDayStyle(_dayB, _selectedDay?.Key == "B");
        _dayA.IsEnabled = _sheet?.Days.Any(day => day.Key == "A") == true;
        _dayB.IsEnabled = _sheet?.Days.Any(day => day.Key == "B") == true;
    }

    private static void SetDayStyle(Button button, bool selected)
    {
        button.BackgroundColor = Color.FromArgb(selected ? "#145A50" : "#E5EFEB");
        button.TextColor = selected ? Colors.White : Color.FromArgb("#145A50");
    }

    private async Task ImportWorkbook()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione sua ficha de treino",
                FileTypes = XlsxFile
            });
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            var imported = await Task.Run(() => WorkoutSheetXlsxReader.Read(stream));
            App.Data.ImportWorkoutSheet(imported);
            _selectedDay = imported.Days.FirstOrDefault(day => day.Key == "A") ?? imported.Days[0];
            _status.Text = "Ficha importada e guardada localmente neste aparelho.";
            ShowSelectedDay();
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("Não foi possível importar a ficha", error.Message, "OK");
        }
    }
}

internal sealed class ExerciseCardSummaryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WorkoutSheetExercise exercise) return "";
        var prescription = string.Join(" · ", new[]
        {
            string.IsNullOrWhiteSpace(exercise.Sets) ? null : $"{exercise.Sets} séries",
            string.IsNullOrWhiteSpace(exercise.Repetitions) ? null : $"{exercise.Repetitions} repetições",
            string.IsNullOrWhiteSpace(exercise.Rest) ? null : $"descanso {exercise.Rest}"
        }.Where(part => part is not null));
        return string.IsNullOrWhiteSpace(exercise.Priority) ? prescription : $"{prescription} · {exercise.Priority}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
