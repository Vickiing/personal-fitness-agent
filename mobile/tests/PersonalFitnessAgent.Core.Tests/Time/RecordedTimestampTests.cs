using PersonalFitnessAgent.Core.Time;

namespace PersonalFitnessAgent.Core.Tests.Time;

public class RecordedTimestampTests
{
    [Fact]
    public void PreservesUtcInstantAndOriginalOffset()
    {
        var utc = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

        var timestamp = new RecordedTimestamp(utc, TimeSpan.FromHours(-3));

        Assert.Equal(utc, timestamp.Utc);
        Assert.Equal(TimeSpan.FromHours(-3), timestamp.OriginalOffset);
    }

    [Fact]
    public void RejectsInstantThatIsNotNormalizedToUtc()
    {
        var local = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<ArgumentException>(() => new RecordedTimestamp(local, TimeSpan.FromHours(-3)));
    }

    [Theory]
    [InlineData(-15)]
    [InlineData(15)]
    public void RejectsOffsetsOutsideTheSupportedRange(int hours)
    {
        var utc = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedTimestamp(utc, TimeSpan.FromHours(hours)));
    }
}
