using System.Globalization;
using PersonalFitnessAgent.Core.Application;

namespace PersonalFitnessAgent.Core.Tests.Application;

public sealed class LocalizedDecimalParserTests
{
    [Fact]
    public void UsesBrazilianDecimalSeparatorWithoutReinterpretingDotAsThousands()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        Assert.True(LocalizedDecimalParser.TryParseNonNegative("12,5", culture, out var parsed));
        Assert.Equal(12.5m, parsed);
        Assert.False(LocalizedDecimalParser.TryParseNonNegative("12.5", culture, out _));
        Assert.False(LocalizedDecimalParser.TryParseNonNegative("1.234", culture, out _));
        Assert.False(LocalizedDecimalParser.TryParseNonNegative("1.234,5", culture, out _));
    }

    [Fact]
    public void UsesEnglishDecimalSeparatorWhenThatIsTheSelectedCulture()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        Assert.True(LocalizedDecimalParser.TryParseNonNegative("12.5", culture, out var parsed));
        Assert.Equal(12.5m, parsed);
        Assert.False(LocalizedDecimalParser.TryParseNonNegative("12,5", culture, out _));
    }

    [Fact]
    public void EmptyInputIsUnknownAndNegativeInputIsRejected()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        Assert.True(LocalizedDecimalParser.TryParseNonNegative("  ", culture, out var missing));
        Assert.Null(missing);
        Assert.False(LocalizedDecimalParser.TryParseNonNegative("-0,1", culture, out _));
    }
}
