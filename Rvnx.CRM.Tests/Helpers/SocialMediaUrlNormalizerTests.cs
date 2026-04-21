using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Helpers;

namespace Rvnx.CRM.Tests.Helpers;

public class SocialMediaUrlNormalizerTests
{
    [Theory]
    [InlineData("not a valid uri")]
    [InlineData("@justausername")]
    [InlineData("justausername")]
    [InlineData("something else entirely")]
    public void ExtractUsernameWhenInvalidUriReturnsTrimmedUsername(string invalidUri)
    {
        string result = SocialMediaUrlNormalizer.ExtractUsername(ContactMethodType.Twitter, invalidUri);

        Assert.Equal(invalidUri.TrimStart('@'), result);
    }

    [Theory]
    [InlineData(ContactMethodType.Twitter, "https://twitter.com/username", "username")]
    [InlineData(ContactMethodType.Twitch, "https://twitch.tv/username", "username")]
    [InlineData(ContactMethodType.YouTube, "https://youtube.com/@username", "username")]
    [InlineData(ContactMethodType.TikTok, "https://tiktok.com/@username", "username")]
    [InlineData(ContactMethodType.Email, "https://github.com/username", "username")]
    public void ExtractUsernameWhenValidUriReturnsExtractedUsername(ContactMethodType type, string url, string expectedUsername)
    {
        string result = SocialMediaUrlNormalizer.ExtractUsername(type, url);

        Assert.Equal(expectedUsername, result);
    }

    [Fact]
    public void ExtractUsernameWhenUrlIsNullOrWhiteSpaceReturnsEmptyString()
    {
        string result = SocialMediaUrlNormalizer.ExtractUsername(ContactMethodType.Twitter, "  ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeWhenUrlIsNullOrWhiteSpaceReturnsInput()
    {
        string result = SocialMediaUrlNormalizer.Normalize(ContactMethodType.Twitter, "  ");

        Assert.Equal("  ", result);
    }

    [Theory]
    [InlineData("http://x.com/username", "http://twitter.com/username")]
    [InlineData("https://x.com/username", "https://twitter.com/username")]
    public void NormalizeWhenTwitterAndXComConvertsToTwitterCom(string input, string expected)
    {
        string result = SocialMediaUrlNormalizer.Normalize(ContactMethodType.Twitter, input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://facebook.com/username")]
    [InlineData("http://linkedin.com/in/username")]
    [InlineData("https://github.com/username")]
    public void NormalizeWhenAlreadyFullUrlReturnsAsIs(string input)
    {
        string result = SocialMediaUrlNormalizer.Normalize(ContactMethodType.Facebook, input);

        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData(ContactMethodType.Twitter, "username", "https://twitter.com/username")]
    [InlineData(ContactMethodType.Twitter, "@username", "https://twitter.com/username")]
    [InlineData(ContactMethodType.Facebook, "username", "https://facebook.com/username")]
    [InlineData(ContactMethodType.Instagram, "username", "https://instagram.com/username")]
    [InlineData(ContactMethodType.Instagram, "@username", "https://instagram.com/username")]
    [InlineData(ContactMethodType.LinkedIn, "username", "https://linkedin.com/in/username")]
    [InlineData(ContactMethodType.GitHub, "username", "https://github.com/username")]
    [InlineData(ContactMethodType.YouTube, "username", "https://youtube.com/@username")]
    [InlineData(ContactMethodType.YouTube, "@username", "https://youtube.com/@username")]
    [InlineData(ContactMethodType.Twitch, "username", "https://twitch.tv/username")]
    [InlineData(ContactMethodType.TikTok, "username", "https://tiktok.com/@username")]
    [InlineData(ContactMethodType.TikTok, "@username", "https://tiktok.com/@username")]
    [InlineData(ContactMethodType.Telegram, "username", "https://t.me/username")]
    [InlineData(ContactMethodType.Telegram, "@username", "https://t.me/username")]
    [InlineData(ContactMethodType.WhatsApp, "1234567890", "https://wa.me/1234567890")]
    [InlineData(ContactMethodType.Skype, "username", "skype:username?chat")]
    [InlineData(ContactMethodType.Email, "test@example.com", "test@example.com")] // default fallback
    public void NormalizeWhenUsernamePrependsCorrectBaseUrl(ContactMethodType type, string input, string expected)
    {
        string result = SocialMediaUrlNormalizer.Normalize(type, input);

        Assert.Equal(expected, result);
    }
}
