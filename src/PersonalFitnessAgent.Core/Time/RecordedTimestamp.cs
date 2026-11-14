namespace PersonalFitnessAgent.Core.Time;

/// <summary>
/// An event instant stored in UTC together with the local offset used when it was recorded.
/// </summary>
public readonly record struct RecordedTimestamp
{
    public DateTimeOffset Utc { get; }
    public TimeSpan OriginalOffset { get; }
    public DateTimeOffset AtOriginalOffset => Utc.ToOffset(OriginalOffset);

    public RecordedTimestamp(DateTimeOffset utc, TimeSpan originalOffset)
    {
        if (utc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The event instant must be normalized to UTC.", nameof(utc));
        }

        if (originalOffset < TimeSpan.FromHours(-14) || originalOffset > TimeSpan.FromHours(14))
        {
            throw new ArgumentOutOfRangeException(nameof(originalOffset), "The original offset must be between -14 and +14 hours.");
        }

        Utc = utc;
        OriginalOffset = originalOffset;
    }
}
