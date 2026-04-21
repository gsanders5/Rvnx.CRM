using Rvnx.CRM.Core.Helpers;

namespace Rvnx.CRM.Tests.Helpers;

public class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("(212) 736-5000", "+12127365000")]
    [InlineData("212-736-5000", "+12127365000")]
    [InlineData("212.736.5000", "+12127365000")]
    [InlineData("+1 212 736 5000", "+12127365000")]
    [InlineData("12127365000", "+12127365000")]
    [InlineData("2127365000", "+12127365000")]
    public void TryNormalizeWhenUsVariantReturnsE164(string input, string expected)
    {
        bool ok = PhoneNumberNormalizer.TryNormalize(input, out string normalized, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("+44 20 7946 0958", "+442079460958")]
    [InlineData("+33 1 42 68 53 00", "+33142685300")]
    public void TryNormalizeWhenInternationalReturnsE164(string input, string expected)
    {
        bool ok = PhoneNumberNormalizer.TryNormalize(input, out string normalized, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("(212) 736-5000 ext. 890", "+12127365000;ext=890")]
    [InlineData("212-736-5000 x123", "+12127365000;ext=123")]
    [InlineData("+12127365000;ext=890", "+12127365000;ext=890")]
    public void TryNormalizeWhenExtensionIncludedPreservesIt(string input, string expected)
    {
        bool ok = PhoneNumberNormalizer.TryNormalize(input, out string normalized, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("not a phone")]
    [InlineData("123")]
    [InlineData("abcdefg")]
    public void TryNormalizeWhenInvalidReturnsFalseWithError(string input)
    {
        bool ok = PhoneNumberNormalizer.TryNormalize(input, out string normalized, out string? error);

        Assert.False(ok);
        Assert.Equal(PhoneNumberNormalizer.InvalidPhoneMessage, error);
        Assert.Equal(input, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalizeWhenEmptyOrWhitespaceReturnsTrueNoError(string? input)
    {
        bool ok = PhoneNumberNormalizer.TryNormalize(input, out string normalized, out string? error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(input ?? string.Empty, normalized);
    }

    [Theory]
    [InlineData("(212) 736-5000")]
    [InlineData("+44 20 7946 0958")]
    [InlineData("(212) 736-5000 ext. 890")]
    public void TryNormalizeIsIdempotent(string input)
    {
        PhoneNumberNormalizer.TryNormalize(input, out string firstPass, out _);
        PhoneNumberNormalizer.TryNormalize(firstPass, out string secondPass, out _);

        Assert.Equal(firstPass, secondPass);
    }

    [Theory]
    [InlineData("+12127365000", "(212) 736-5000")]
    [InlineData("+12127365000;ext=890", "(212) 736-5000 ext. 890")]
    public void FormatForDisplayWhenUsReturnsNational(string stored, string expected)
    {
        string result = PhoneNumberNormalizer.FormatForDisplay(stored);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatForDisplayWhenInternationalReturnsInternational()
    {
        string result = PhoneNumberNormalizer.FormatForDisplay("+442079460958");

        Assert.Equal("+44 20 7946 0958", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FormatForDisplayWhenEmptyReturnsEmpty(string? input)
    {
        string result = PhoneNumberNormalizer.FormatForDisplay(input);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatForTelUriReturnsRfc3966WithTelPrefix()
    {
        string result = PhoneNumberNormalizer.FormatForTelUri("+12127365000");

        Assert.Equal("tel:+1-212-736-5000", result);
    }

    [Fact]
    public void FormatForTelUriWithExtensionIncludesExtension()
    {
        string result = PhoneNumberNormalizer.FormatForTelUri("+12127365000;ext=890");

        Assert.StartsWith("tel:", result);
        Assert.Contains("ext=890", result);
    }
}
