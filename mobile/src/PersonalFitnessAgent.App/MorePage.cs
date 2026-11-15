using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class MorePage : ContentPage
{
    public MorePage()
    {
        Title = "Mais";
        BackgroundColor = Color.FromArgb("#F4F6F2");
        var title = Ui.Heading("Seu acompanhamento");
        title.TextColor = Color.FromArgb("#14221F");
        var intro = new Label
        {
            Text = "Acesse seus dados, preferências e informações de segurança.",
            FontSize = 14,
            TextColor = Color.FromArgb("#71807B"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 24),
            Spacing = 14,
            Children =
            {
                title,
                intro,
                LinkCard("Evolução", "Resumo semanal e tendências", async () => await Shell.Current.Navigation.PushAsync(new ProgressPage())),
                LinkCard("Restrições", "Políticas e revisões de segurança", async () => await Shell.Current.Navigation.PushAsync(new RestrictionsPage())),
                new Label
                {
                    Text = "Se houver conflito entre uma preferência e uma orientação clínica, siga a orientação do profissional e revise a política no app.",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#71807B"),
                    Margin = new Thickness(2, 8, 2, 0),
                    LineBreakMode = LineBreakMode.WordWrap
                }
            }
        };
        Content = new ScrollView { Content = content, BackgroundColor = BackgroundColor };
    }

    private static View LinkCard(string title, string subtitle, Func<Task> action)
    {
        var labels = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#14221F") },
                new Label { Text = subtitle, FontSize = 13, TextColor = Color.FromArgb("#71807B") }
            }
        };
        var arrow = new Label
        {
            Text = "›",
            FontSize = 30,
            TextColor = Color.FromArgb("#597268"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            WidthRequest = 36
        };
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            Children = { labels, arrow }
        };
        grid.SetColumn(arrow, 1);
        var card = new Border
        {
            Stroke = Color.FromArgb("#E4E9E3"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 10),
            Content = grid
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await action()) });
        SemanticProperties.SetDescription(card, $"{title}. {subtitle}");
        return card;
    }
}
