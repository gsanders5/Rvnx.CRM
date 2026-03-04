using Rvnx.CRM.Core.Enumerations;
using System;

namespace Rvnx.CRM.Web.Helpers
{
    public static class SocialMediaEmbedHelper
    {
        public static string ExtractUsername(ContactMethodType type, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            try
            {
                var uri = new Uri(url);
                string path = uri.AbsolutePath.Trim('/');

                return type switch
                {
                    ContactMethodType.Twitter => path,
                    ContactMethodType.Twitch => path,
                    ContactMethodType.YouTube => path.StartsWith('@') ? path.Substring(1) : path,
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
