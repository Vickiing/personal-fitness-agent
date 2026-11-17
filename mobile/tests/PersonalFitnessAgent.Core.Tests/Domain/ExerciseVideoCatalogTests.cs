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
            Assert.Equal(clip.Playback == ExerciseVideoPlayback.YouTubeEmbed, clip.VideoMimeType is null);
        });
    }

    [Theory]
    [InlineData(ExerciseMotion.SupportedRow, "0-DXJiceG-0")]
    [InlineData(ExerciseMotion.ReverseFly, "vJYkqD7a0gM")]
    public void YouTubeExercisesUseThePrivacyEnhancedEmbed(ExerciseMotion motion, string videoId)
    {
        var clip = ExerciseVideoCatalog.For(motion);

        Assert.NotNull(clip);
        Assert.Equal(ExerciseVideoPlayback.YouTubeEmbed, clip.Playback);
        Assert.Contains($"youtube-nocookie.com/embed/{videoId}", clip.VideoUrl, StringComparison.Ordinal);
    }
}
