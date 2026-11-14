using System.Globalization;
using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

internal static class Ui
{
    private const string Ink = "#202A29";
    private const string Muted = "#64716E";
    private const string Accent = "#145A50";

    public static Entry Input(string placeholder, Keyboard? keyboard = null) => new()
    {
        Placeholder = placeholder,
        Keyboard = keyboard ?? Keyboard.Default,
        BackgroundColor = Colors.White,
        TextColor = Color.FromArgb(Ink),
        PlaceholderColor = Color.FromArgb(Muted),
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
        Margin = new Thickness(0, 3)
    };

    public static Button Button(string text, EventHandler handler, bool secondary = false)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(secondary ? "#E5EFEB" : Accent),
            TextColor = secondary ? Color.FromArgb(Accent) : Colors.White,
            CornerRadius = 14,
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 48,
            HorizontalOptions = LayoutOptions.Fill
        };
        button.Clicked += handler;
        return button;
    }

    public static Label Heading(string text)
    {
        var label = new Label { Text = text, FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(Ink) };
        SemanticProperties.SetHeadingLevel(label, SemanticHeadingLevel.Level1);
        return label;
    }

    public static Label Section(string text)
    {
        var label = new Label { Text = text, FontSize = 17, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb(Accent), Margin = new Thickness(0, 12, 0, 0) };
        SemanticProperties.SetHeadingLevel(label, SemanticHeadingLevel.Level2);
        return label;
    }

    public static Label Body(string text) => new()
    {
        Text = text,
        FontSize = 15,
        TextColor = Color.FromArgb(Ink),
        LineBreakMode = LineBreakMode.WordWrap
    };

    public static VerticalStackLayout Form(string title, params View[] children)
    {
        var stack = new VerticalStackLayout { Padding = new Thickness(22, 18), Spacing = 12 };
        stack.Children.Add(Heading(title));
        foreach (var child in children) stack.Children.Add(child);
        return stack;
    }

    public static ScrollView Scroll(string title, params View[] children) => new()
    {
        Content = Form(title, children),
        BackgroundColor = Color.FromArgb("#F5F4EF")
    };

    public static decimal? Decimal(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var number) || number < 0)
            throw new FormatException($"{field}: informe um número válido usando o separador decimal do aparelho.");
        return number;
    }

    public static int? Integer(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number) || number < 0)
            throw new FormatException($"{field}: informe um número inteiro igual ou maior que zero.");
        return number;
    }

    public static async Task ShowError(Page page, Exception error) =>
        await page.DisplayAlertAsync("Não foi possível salvar", error.Message, "OK");
}
