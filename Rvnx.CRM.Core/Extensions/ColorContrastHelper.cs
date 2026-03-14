namespace Rvnx.CRM.Core.Extensions;

public static class ColorContrastHelper
{
    public static string GetContrastTextColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor.Length < 4)
        {
            return "#000000";
        }

        string hex = hexColor.StartsWith('#') ? hexColor[1..] : hexColor;

        if (hex.Length == 3)
        {
            hex = new string([hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]]);
        }

        if (hex.Length != 6)
        {
            return "#000000";
        }

        try
        {
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            double[] rsRGB = [r / 255.0, g / 255.0, b / 255.0];
            for (int i = 0; i < 3; i++)
            {
                rsRGB[i] = rsRGB[i] <= 0.03928 ? rsRGB[i] / 12.92 : Math.Pow((rsRGB[i] + 0.055) / 1.055, 2.4);
            }

            double luminance = (0.2126 * rsRGB[0]) + (0.7152 * rsRGB[1]) + (0.0722 * rsRGB[2]);

            return luminance > 0.179 ? "#000000" : "#ffffff";
        }
        catch
        {
            return "#000000";
        }
    }
}