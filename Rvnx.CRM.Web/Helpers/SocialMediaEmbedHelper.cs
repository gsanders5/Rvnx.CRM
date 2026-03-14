using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Web.Helpers
{
    public static class SocialMediaEmbedHelper
    {
        public static string ExtractUsername(ContactMethodType type, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            try
            {
                Uri uri = new(url);
                string path = uri.AbsolutePath.Trim('/');

                return type switch
                {
                    ContactMethodType.Twitter => path,
                    ContactMethodType.Twitch => path,
                    ContactMethodType.YouTube => path.StartsWith('@') ? path[1..] : path,
                    ContactMethodType.TikTok => path.StartsWith('@') ? path[1..] : path,
                    _ => path
                };
            }
            catch
            {
                // Not a valid URI, assume it's just a username
                return url.TrimStart('@');
            }
        }
    }
}