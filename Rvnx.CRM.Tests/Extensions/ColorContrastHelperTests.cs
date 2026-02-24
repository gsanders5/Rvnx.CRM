using FluentAssertions;
using Rvnx.CRM.Core.Extensions;

namespace Rvnx.CRM.Tests.Extensions;

public class ColorContrastHelperTests
{
    [Theory]
    [InlineData("#ffffff", "#000000")] // White bg -> Black text
    [InlineData("#000000", "#ffffff")] // Black bg -> White text
    [InlineData("#ff0000", "#000000")] // Red bg -> Black text
    [InlineData("#00ff00", "#000000")] // Green bg -> Black text
    [InlineData("#0000ff", "#ffffff")] // Blue bg -> White text
    [InlineData("#e9ecef", "#000000")] // Light gray bg -> Black text
    [InlineData("#343a40", "#ffffff")] // Dark gray bg -> White text
    [InlineData("invalid", "#000000")] // Invalid input defaults to Black text
    [InlineData(null, "#000000")]      // Null input defaults to Black text
    [InlineData("#123", "#ffffff")]    // 3-char dark hex
    [InlineData("#abc", "#000000")]    // 3-char light hex
    public void GetContrastTextColorReturnsExpectedColor(string? bg, string expectedText)
    {
        string result = ColorContrastHelper.GetContrastTextColor(bg);
        result.Should().Be(expectedText);
    }
}
