using System;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Web.Helpers;
using Xunit;

namespace Rvnx.CRM.Tests.Helpers
{
    public class SocialMediaEmbedHelperTests
    {
        [Theory]
        [InlineData("not a valid uri")]
        [InlineData("@justausername")]
        [InlineData("justausername")]
        [InlineData("something else entirely")]
        public void ExtractUsernameWhenInvalidUriReturnsTrimmedUsername(string invalidUri)
        {
            // Act
            var result = SocialMediaEmbedHelper.ExtractUsername(ContactMethodType.Twitter, invalidUri);

            // Assert
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
            // Act
            var result = SocialMediaEmbedHelper.ExtractUsername(type, url);

            // Assert
            Assert.Equal(expectedUsername, result);
        }

        [Fact]
        public void ExtractUsernameWhenUrlIsNullOrWhiteSpaceReturnsEmptyString()
        {
            // Act
            var result = SocialMediaEmbedHelper.ExtractUsername(ContactMethodType.Twitter, "  ");

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
