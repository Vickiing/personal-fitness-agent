using Microsoft.Maui.Graphics;

namespace PersonalFitnessAgent.App;

public sealed class MorePage : ContentPage
{
    public MorePage()
    {
        Title = "Minha conta";
        BackgroundColor = Color.FromArgb("#0B0F14");
        var title = Ui.Heading("Seu acompanhamento");
        title.TextColor = Color.FromArgb("#F3F6FA");
        var intro = new Label
        {
            Text = "Acesse seus check-ins, acompanhamento, restrições e controles dos seus dados.",
            FontSize = 14,
            TextColor = Color.FromArgb("#A7B2BF"),
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
                Ui.Section("Dados pessoais"),
                LinkCard("Check-in", "Sono, alimentação, medidas e sintomas", async () => await Shell.Current.Navigation.PushAsync(new CheckInPage())),
                Ui.Section("Acompanhamento"),
                LinkCard("Evolução e agenda", "Resumo semanal e dias planejados", async () => await Shell.Current.Navigation.PushAsync(new ProgressPage())),
                Ui.Section("Segurança e privacidade"),
                LinkCard("Restrições", "Políticas e revisões de segurança", async () => await Shell.Current.Navigation.PushAsync(new RestrictionsPage())),
                LinkCard("Privacidade e dados", "Backup, restauração e dados locais", async () => await Shell.Current.Navigation.PushAsync(new PrivacyDataPage())),
                new Label
                {
                    Text = "Se houver conflito entre uma preferência e uma orientação clínica, siga a orientação do profissional e revise a política no app.",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#A7B2BF"),
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
                new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#F3F6FA") },
                new Label { Text = subtitle, FontSize = 13, TextColor = Color.FromArgb("#A7B2BF") }
            }
        };
        var arrow = new Label
        {
            Text = "›",
            FontSize = 30,
            TextColor = Color.FromArgb("#FF8A52"),
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
            Stroke = Color.FromArgb("#293440"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = new Thickness(16, 10),
            Content = grid
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await action()) });
        SemanticProperties.SetDescription(card, $"{title}. {subtitle}");
        return card;
    }
}
