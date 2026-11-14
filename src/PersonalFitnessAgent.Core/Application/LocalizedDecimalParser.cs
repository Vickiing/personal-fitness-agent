using System.Globalization;

namespace PersonalFitnessAgent.Core.Application;

/// <summary>Parses a decimal using only the decimal separator for the supplied culture; grouping is rejected.</summary>
public static class LocalizedDecimalParser
{
    private const NumberStyles Styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParseNonNegative(string? text, CultureInfo culture, out decimal? value)
    {
        ArgumentNullException.ThrowIfNull(culture);
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!decimal.TryParse(text, Styles, culture, out var parsed) || parsed < 0) return false;
        value = parsed;
        return true;
    }
}
