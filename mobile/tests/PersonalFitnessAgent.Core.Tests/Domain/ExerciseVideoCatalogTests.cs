using PersonalFitnessAgent.App;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.Core.Tests.Domain;

public sealed class ExerciseVideoCatalogTests
{
    [Fact]
    public void EveryExerciseMotionHasAPlayableVideoWithSourceAndLicense()
    {
        var motions = Enum.GetValues<ExerciseMotion>();
        var clips = ExerciseVideoCatalog.All.ToDictionary(clip => clip.Motion);

        Assert.Equal(motions.Length, clips.Count);
        Assert.All(motions, motion => Assert.True(clips.ContainsKey(motion), $"Missing video for {motion}."));

        Assert.All(clips.Values, clip =>
        {
            Assert.False(string.IsNullOrWhiteSpace(clip.Title));
            Assert.StartsWith("https://", clip.VideoUrl);
            Assert.StartsWith("https://", clip.PosterUrl);
            Assert.False(string.IsNullOrWhiteSpace(clip.Creator));
            Assert.StartsWith("https://", clip.SourcePageUrl);
            Assert.False(string.IsNullOrWhiteSpace(clip.LicenseName));
            Assert.StartsWith("https://", clip.LicenseUrl);
            Assert.Equal(ExerciseVideoPlayback.Html5, clip.Playback);
            Assert.Contains(clip.VideoMimeType, new[] { "video/mp4", "video/webm" });
        });
    }

    [Theory]
    [InlineData("Remada sentada com apoio no peito", ExerciseMotion.SupportedRow)]
    [InlineData("Remada sentada na polia", ExerciseMotion.CableRow)]
    [InlineData("Crucifixo na máquina", ExerciseMotion.ChestFly)]
    [InlineData("Elevação lateral sentada", ExerciseMotion.LateralRaise)]
    [InlineData("Cadeira extensora", ExerciseMotion.LegExtension)]
    [InlineData("Extensão de tríceps na polia", ExerciseMotion.TricepsPushdown)]
    [InlineData("Agachamento sem peso", ExerciseMotion.BodyweightSquat)]
    [InlineData("Puxada frontal na polia", ExerciseMotion.LatPulldown)]
    [InlineData("Cadeira flexora", ExerciseMotion.LegCurl)]
    [InlineData("Voador inverso", ExerciseMotion.ReverseFly)]
    [InlineData("Passada sem peso", ExerciseMotion.BodyweightLunge)]
    [InlineData("Rosca sentada com halteres", ExerciseMotion.SeatedCurl)]
    public void CommonPortugueseExerciseNamesResolveToMovementsWithVideos(string name, ExerciseMotion expectedMotion)
    {
        var guide = ExerciseGuideCatalog.For(name);

        Assert.Equal(expectedMotion, guide.Motion);
        Assert.NotNull(ExerciseVideoCatalog.For(guide.Motion));
    }

    [Fact]
    public void ReplacedYouTubeEmbedsUseDirectPlayableVideoFiles()
    {
        var supportedRow = ExerciseVideoCatalog.For(ExerciseMotion.SupportedRow);
        var reverseFly = ExerciseVideoCatalog.For(ExerciseMotion.ReverseFly);

        Assert.NotNull(supportedRow);
        Assert.NotNull(reverseFly);
        Assert.Equal(ExerciseVideoPlayback.Html5, supportedRow.Playback);
        Assert.Equal(ExerciseVideoPlayback.Html5, reverseFly.Playback);
        Assert.Equal("video/mp4", supportedRow.VideoMimeType);
        Assert.Equal("video/mp4", reverseFly.VideoMimeType);
        Assert.Contains("videos.pexels.com/video-files/", supportedRow.VideoUrl, StringComparison.Ordinal);
        Assert.Contains(".360p.mpeg4.mov", reverseFly.VideoUrl, StringComparison.Ordinal);
    }
}
