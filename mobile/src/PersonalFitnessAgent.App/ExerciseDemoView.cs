using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>
/// Shows open licensed live-action clips where an exact movement match is available.
/// These clips stream from Wikimedia Commons and require an internet connection.
/// </summary>
public sealed class ExerciseDemoView : ContentView
{
    private const string LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0/";
    public ExerciseDemoView(ExerciseGuide guide)
    {
        ArgumentNullException.ThrowIfNull(guide);

        var clip = FindClip(guide.Motion);
        View content;
        if (clip is null)
        {
            content = new Border
            {
                HeightRequest = 250,
                Stroke = Color.FromArgb("#344252"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
                BackgroundColor = Color.FromArgb("#151C24"),
                Padding = 24,
                Content = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "VÍDEO EM BREVE", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF642D"), HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Demonstração em vídeo indisponível para este movimento.", FontSize = 14, TextColor = Color.FromArgb("#CFD8E2"), HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap }
                    }
                }
            };
        }
        else
        {
            var player = new WebView
            {
                HeightRequest = 255,
                BackgroundColor = Color.FromArgb("#151C24"),
                Source = new HtmlWebViewSource
                {
                    Html = $"""
                    <!doctype html>
                    <html><head><meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1"></head>
                    <body style="margin:0;background-color:#151C24;color:#CFD8E2">
                      <video poster="{clip.PosterUrl}" style="width:100%;height:255px;object-fit:contain;background-color:#151C24" controls autoplay muted loop playsinline>
                        <source src="{clip.VideoUrl}" type="video/webm">
                        Este aparelho não conseguiu carregar o vídeo.
                      </video>
                    </body></html>
                    """
                }
            };
            var creditText = new FormattedString();
            creditText.Spans.Add(new Span { Text = $"Loop em vídeo · {clip.Title}\nFonte: {clip.Author} · " });
            creditText.Spans.Add(new Span
            {
                Text = "Wikimedia Commons",
                TextColor = Color.FromArgb("#FF8A52"),
                TextDecorations = TextDecorations.Underline,
                GestureRecognizers = { new TapGestureRecognizer { Command = new Command(async () => await Browser.Default.OpenAsync(clip.CommonsPageUrl)) } }
            });
            creditText.Spans.Add(new Span { Text = " · " });
            creditText.Spans.Add(new Span
            {
                Text = "CC BY-SA 4.0",
                TextColor = Color.FromArgb("#FF8A52"),
                TextDecorations = TextDecorations.Underline,
                GestureRecognizers = { new TapGestureRecognizer { Command = new Command(async () => await Browser.Default.OpenAsync(LicenseUrl)) } }
            });
            creditText.Spans.Add(new Span { Text = " · requer internet" });
            var credit = new Label
            {
                FormattedText = creditText,
                FontSize = 11,
                TextColor = Color.FromArgb("#A7B2BF"),
                LineBreakMode = LineBreakMode.WordWrap,
                Margin = new Thickness(2, 6, 2, 0)
            };
            var sourceButton = new Button
            {
                Text = "Abrir vídeo",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FF8A52"),
                BackgroundColor = Colors.Transparent,
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.Start,
                Padding = new Thickness(4, 0)
            };
            sourceButton.Clicked += async (_, _) => await Browser.Default.OpenAsync(clip.CommonsPageUrl);
            SemanticProperties.SetDescription(player, guide.ImageDescription);
            content = new VerticalStackLayout { Spacing = 0, Children = { player, credit, sourceButton } };
        }

        Content = new Border
        {
            Stroke = Color.FromArgb("#344252"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Color.FromArgb("#151C24"),
            Padding = 10,
            Content = content
        };
        SemanticProperties.SetDescription(this, guide.ImageDescription);
    }

    private static LicensedClip? FindClip(ExerciseMotion motion) => motion switch
    {
        ExerciseMotion.LatPulldown => new LicensedClip(
            "Puxada na polia",
            "https://upload.wikimedia.org/wikipedia/commons/6/69/Lat_pulldown_exercise_demonstration.webm",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/6/69/Lat_pulldown_exercise_demonstration.webm/500px--Lat_pulldown_exercise_demonstration.webm.jpg",
            "Your Move",
            "https://commons.wikimedia.org/wiki/File:Lat_pulldown_exercise_demonstration.webm"),
        ExerciseMotion.LegExtension => new LicensedClip(
            "Cadeira extensora",
            "https://upload.wikimedia.org/wikipedia/commons/8/8f/Leg_extension_exercise_demonstration.webm",
            "https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8f/Leg_extension_exercise_demonstration.webm/500px--Leg_extension_exercise_demonstration.webm.jpg",
            "Your Move",
            "https://commons.wikimedia.org/wiki/File:Leg_extension_exercise_demonstration.webm"),
        _ => null
    };

    private sealed record LicensedClip(string Title, string VideoUrl, string PosterUrl, string Author, string CommonsPageUrl);
}
