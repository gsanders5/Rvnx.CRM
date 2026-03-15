using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Helpers;

namespace Rvnx.CRM.Tests.Helpers
{
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
    }
}