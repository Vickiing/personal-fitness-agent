using System.Net;
using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>
/// Plays licensed live-action exercise videos in the workout guide.
/// Videos stream from their source and require an internet connection.
/// </summary>
public sealed class ExerciseDemoView : ContentView
{
    public static bool HasVideoFor(ExerciseMotion motion) => ExerciseVideoCatalog.For(motion) is not null;

    public static string? PreviewUrlFor(ExerciseMotion motion) => ExerciseVideoCatalog.For(motion)?.PosterUrl;

    public ExerciseDemoView(ExerciseGuide guide)
    {
        ArgumentNullException.ThrowIfNull(guide);

        var clip = ExerciseVideoCatalog.For(guide.Motion);
        View content;
        if (clip is null)
        {
            content = new Border
            {
                HeightRequest = 94,
                Stroke = Color.FromArgb("#344252"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
                BackgroundColor = Color.FromArgb("#151C24"),
                Padding = 18,
                Content = new Label
                {
                    Text = "Demonstração em vídeo indisponível.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#A7B2BF"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };
        }
        else
        {
            var player = new WebView
            {
                HeightRequest = 255,
                BackgroundColor = Color.FromArgb("#151C24"),
                Source = new HtmlWebViewSource { Html = BuildPlayerHtml(clip) }
            };

            var creditText = new FormattedString();
            creditText.Spans.Add(new Span { Text = $"{clip.Creator} · " });
            creditText.Spans.Add(Link(clip.SourceName, clip.SourcePageUrl));
            creditText.Spans.Add(new Span { Text = " · " });
            creditText.Spans.Add(Link(clip.LicenseName, clip.LicenseUrl));
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
                Text = "Fonte do vídeo",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FF8A52"),
                BackgroundColor = Colors.Transparent,
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.Start,
                Padding = new Thickness(4, 0)
            };
            sourceButton.Clicked += async (_, _) => await Browser.Default.OpenAsync(clip.SourcePageUrl);
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

    private static string BuildPlayerHtml(ExerciseVideoAsset clip)
    {
        var player = $"""
            <video poster="{WebUtility.HtmlEncode(clip.PosterUrl ?? string.Empty)}"
                   style="width:100%;height:255px;object-fit:contain;background-color:#151C24"
                   controls autoplay muted loop playsinline preload="metadata"
                   onerror="document.getElementById('fallback').style.display='block'">
              <source src="{WebUtility.HtmlEncode(clip.VideoUrl)}" type="{WebUtility.HtmlEncode(clip.VideoMimeType ?? "video/mp4")}">
              Este aparelho não conseguiu carregar o vídeo.
            </video>
            <div id="fallback" style="display:none;text-align:center;padding:12px">
              Vídeo indisponível. Use Fonte do vídeo.
            </div>
            """;

        return $"""
            <!doctype html>
            <html>
              <head>
                <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1">
              </head>
              <body style="margin:0;background-color:#151C24;color:#CFD8E2;overflow:hidden">
                {player}
              </body>
            </html>
            """;
    }

    private static Span Link(string text, string url) => new()
    {
        Text = text,
        TextColor = Color.FromArgb("#FF8A52"),
        TextDecorations = TextDecorations.Underline,
        GestureRecognizers =
        {
            new TapGestureRecognizer { Command = new Command(async () => await Browser.Default.OpenAsync(url)) }
        }
    };
}
