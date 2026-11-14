using Microsoft.Maui.Graphics;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.App;

/// <summary>Lightweight, offline vector illustration with an on-demand movement animation.</summary>
public sealed class ExerciseDemoView : ContentView
{
    private readonly DemoDrawable _drawable;
    private readonly GraphicsView _graphics;
    private readonly Button _play;
    private readonly IDispatcherTimer _timer;
    private float _progress;
    private bool _playing;

    public ExerciseDemoView(ExerciseGuide guide)
    {
        _drawable = new DemoDrawable(guide.Motion);
        _graphics = new GraphicsView
        {
            Drawable = _drawable,
            HeightRequest = 260,
            BackgroundColor = Color.FromArgb("#F1F5F1")
        };
        SemanticProperties.SetDescription(_graphics, guide.ImageDescription);
        _play = Ui.Button("Reproduzir demonstração", (_, _) => Play(), secondary: true);
        AutomationProperties.SetHelpText(_play, "Reproduz uma animação vetorial curta do movimento. O vídeo não começa automaticamente.");
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => Advance();
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            _playing = false;
        };
        Content = new Border
        {
            Stroke = Color.FromArgb("#E1E6E1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Colors.White,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Ui.Section("Imagem e demonstração"),
                    _graphics,
                    Ui.Body("Ilustração vetorial local. Toque para reproduzir; não usa Internet e não inicia sozinha."),
                    _play
                }
            }
        };
    }

    private void Play()
    {
        _progress = 0;
        _playing = true;
        _play.Text = "Reproduzindo…";
        _play.IsEnabled = false;
        _drawable.Progress = _progress;
        _graphics.Invalidate();
        _timer.Start();
    }

    private void Advance()
    {
        if (!_playing) return;
        _progress = Math.Min(1f, _progress + 0.025f);
        _drawable.Progress = _progress;
        _graphics.Invalidate();
        if (_progress < 1f) return;
        _timer.Stop();
        _playing = false;
        _play.Text = "Repetir demonstração";
        _play.IsEnabled = true;
    }

    private sealed class DemoDrawable(ExerciseMotion motion) : IDrawable
    {
        public float Progress { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var center = width * 0.5f;
            canvas.SaveState();
            canvas.FillColor = Color.FromArgb("#F1F5F1");
            canvas.FillRoundedRectangle(dirtyRect, 16);
            canvas.StrokeColor = Color.FromArgb("#CBD7D0");
            canvas.StrokeSize = 3;
            canvas.DrawLine(24, height - 25, width - 24, height - 25);
            DrawEquipment(canvas, center, height);
            DrawPerson(canvas, center, height);
            canvas.FillColor = Color.FromArgb("#64716E");
            canvas.FontSize = 12;
            var phase = Progress < 0.5f ? "POSIÇÃO INICIAL" : "POSIÇÃO FINAL";
            canvas.DrawString(phase, 8, height - 22, width - 16, 18,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
        }

        private void DrawEquipment(ICanvas canvas, float center, float height)
        {
            canvas.StrokeColor = Color.FromArgb("#8A9992");
            canvas.StrokeSize = 5;
            switch (motion)
            {
                case ExerciseMotion.CableRow:
                    canvas.DrawLine(center + 85, 82, center + 85, height - 27);
                    canvas.DrawLine(center + 85, 145, center + 28, 145);
                    break;
                case ExerciseMotion.LatPulldown:
                    canvas.DrawLine(center + 82, 24, center + 82, 96);
                    canvas.DrawLine(center + 44, 24, center + 82, 24);
                    canvas.DrawLine(center + 60, 35, center + 101, 35);
                    break;
                case ExerciseMotion.ChestFly:
                case ExerciseMotion.ReverseFly:
                case ExerciseMotion.SupportedRow:
                    canvas.DrawLine(center - 42, 121, center + 42, 121);
                    canvas.DrawLine(center - 42, 121, center - 42, 158);
                    canvas.DrawLine(center + 42, 121, center + 42, 158);
                    break;
                case ExerciseMotion.LegExtension:
                case ExerciseMotion.LegCurl:
                    canvas.DrawLine(center - 20, 157, center + 58, 157);
                    canvas.DrawLine(center + 58, 157, center + 58, 208);
                    break;
                case ExerciseMotion.TricepsPushdown:
                    canvas.DrawLine(center + 76, 48, center + 76, height - 27);
                    canvas.DrawLine(center + 62, 48, center + 90, 48);
                    break;
            }
        }

        private void DrawPerson(ICanvas canvas, float center, float height)
        {
            var p = Ease(Math.Clamp(Progress, 0f, 1f));
            var shoulderY = 81f;
            var hipY = 157f;
            var squatDrop = motion == ExerciseMotion.BodyweightSquat ? p * 24f : 0f;
            var hipX = center;
            var hip = new PointF(hipX, hipY + squatDrop);
            var shoulder = new PointF(center, shoulderY + squatDrop * 0.55f);
            var neck = new PointF(center, shoulder.Y - 16);

            canvas.StrokeColor = Color.FromArgb("#145A50");
            canvas.StrokeSize = 10;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(neck.X, neck.Y, hip.X, hip.Y);
            canvas.DrawLine(center - 18, shoulder.Y, center + 18, shoulder.Y);
            canvas.FillColor = Color.FromArgb("#F0C8A6");
            canvas.FillCircle(center, neck.Y - 18, 13);

            DrawArms(canvas, center, shoulder.Y, hip.Y, p);
            DrawLegs(canvas, center, hip, p, height);
        }

        private void DrawArms(ICanvas canvas, float center, float shoulderY, float hipY, float p)
        {
            var (elbowOut, handOut, elbowY, handY) = motion switch
            {
                ExerciseMotion.SupportedRow or ExerciseMotion.CableRow =>
                    (Lerp(73, 38, p), Lerp(103, 62, p), shoulderY + 29, shoulderY + 37),
                ExerciseMotion.ChestFly =>
                    (Lerp(75, 20, p), Lerp(96, 31, p), shoulderY + 4, shoulderY + 15),
                ExerciseMotion.ReverseFly =>
                    (Lerp(20, 75, p), Lerp(31, 96, p), shoulderY + 4, shoulderY + 15),
                ExerciseMotion.LateralRaise =>
                    (Lerp(34, 64, p), Lerp(34, 84, p), shoulderY + 35, shoulderY - 2),
                ExerciseMotion.LatPulldown =>
                    (Lerp(72, 46, p), Lerp(83, 31, p), Lerp(shoulderY - 31, shoulderY + 12, p), Lerp(shoulderY - 49, shoulderY + 39, p)),
                ExerciseMotion.TricepsPushdown =>
                    (25, 24, shoulderY + 26, Lerp(shoulderY + 48, hipY + 26, p)),
                ExerciseMotion.SeatedCurl =>
                    (23, 25, shoulderY + 40, Lerp(hipY + 54, shoulderY + 53, p)),
                _ =>
                    (28, 34, shoulderY + 40, shoulderY + 53)
            };

            canvas.StrokeColor = Color.FromArgb("#145A50");
            canvas.StrokeSize = 8;
            canvas.StrokeLineCap = LineCap.Round;
            foreach (var side in new[] { -1f, 1f })
            {
                var elbow = new PointF(center + side * elbowOut, elbowY);
                var wrist = new PointF(center + side * handOut, handY);
                canvas.DrawLine(center + side * 15, shoulderY + 2, elbow.X, elbow.Y);
                canvas.DrawLine(elbow.X, elbow.Y, wrist.X, wrist.Y);
                canvas.FillColor = Color.FromArgb("#F0C8A6");
                canvas.FillCircle(wrist.X, wrist.Y, 5);
            }
        }

        private void DrawLegs(ICanvas canvas, float center, PointF hip, float p, float height)
        {
            canvas.StrokeColor = Color.FromArgb("#34413D");
            canvas.StrokeSize = 10;
            canvas.StrokeLineCap = LineCap.Round;
            if (motion == ExerciseMotion.LegExtension || motion == ExerciseMotion.LegCurl)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var knee = new PointF(center + side * 13, hip.Y + 24);
                    var foot = motion == ExerciseMotion.LegExtension
                        ? new PointF(center + side * 38, hip.Y + 45 - 26 * p)
                        : new PointF(center + side * 47, hip.Y + 48 - 22 * p);
                    canvas.DrawLine(hip.X + side * 8, hip.Y, knee.X, knee.Y);
                    canvas.DrawLine(knee.X, knee.Y, foot.X, foot.Y);
                }
                return;
            }
            if (motion == ExerciseMotion.BodyweightLunge)
            {
                var leadKnee = new PointF(center - 20, hip.Y + 40);
                var leadFoot = new PointF(center - 24, height - 29);
                var backKnee = new PointF(Lerp(center + 22, center + 42, p), hip.Y + 32);
                var backFoot = new PointF(Lerp(center + 28, center + 78, p), height - 29);
                canvas.DrawLine(hip.X - 8, hip.Y, leadKnee.X, leadKnee.Y);
                canvas.DrawLine(leadKnee.X, leadKnee.Y, leadFoot.X, leadFoot.Y);
                canvas.DrawLine(hip.X + 8, hip.Y, backKnee.X, backKnee.Y);
                canvas.DrawLine(backKnee.X, backKnee.Y, backFoot.X, backFoot.Y);
                return;
            }

            var kneeY = motion == ExerciseMotion.BodyweightSquat ? hip.Y + 38 : hip.Y + 48;
            var kneeSpacing = motion == ExerciseMotion.BodyweightSquat ? Lerp(24, 38, p) : 20;
            foreach (var side in new[] { -1f, 1f })
            {
                var knee = new PointF(center + side * kneeSpacing, kneeY);
                var foot = new PointF(center + side * 30, height - 29);
                canvas.DrawLine(hip.X + side * 8, hip.Y, knee.X, knee.Y);
                canvas.DrawLine(knee.X, knee.Y, foot.X, foot.Y);
            }
        }

        private static float Lerp(float start, float end, float amount) => start + (end - start) * amount;
        private static float Ease(float amount) => amount * amount * (3f - 2f * amount);
    }
}
