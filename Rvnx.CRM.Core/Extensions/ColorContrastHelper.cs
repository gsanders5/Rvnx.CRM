using System.Globalization;

namespace Rvnx.CRM.Core.Extensions;

public static class ColorContrastHelper
{
    private const string Black = "#000000";
    private const string White = "#ffffff";

    public static string GetContrastTextColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            return Black;
        }

        ReadOnlySpan<char> hex = hexColor.AsSpan();
        if (hex[0] == '#')
        {
            hex = hex[1..];
        }

        // Expand the 3-char shorthand (e.g. "#abc" -> "aabbcc") without allocating.
        if (hex.Length == 3)
        {
            Span<char> expanded = stackalloc char[6];
            expanded[0] = expanded[1] = hex[0];
            expanded[2] = expanded[3] = hex[1];
            expanded[4] = expanded[5] = hex[2];
            return ComputeContrastForSixChars(expanded);
        }

        return hex.Length == 6 ? ComputeContrastForSixChars(hex) : Black;
    }

    private static string ComputeContrastForSixChars(ReadOnlySpan<char> hex)
    {
        if (!byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r)
            || !byte.TryParse(hex.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g)
            || !byte.TryParse(hex.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return Black;
        }

        double luminance = (0.2126 * Linearize(r)) + (0.7152 * Linearize(g)) + (0.0722 * Linearize(b));
        return luminance > 0.179 ? Black : White;
    }

    private static double Linearize(byte channel)
    {
        double c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
