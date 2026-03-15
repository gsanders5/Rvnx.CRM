using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.Helpers
{
    public static class SocialMediaUrlNormalizer
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

        public static string Normalize(ContactMethodType type, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = value.Trim();

            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? type == ContactMethodType.Twitter && value.Contains("x.com", StringComparison.OrdinalIgnoreCase)
                    ? value.Replace("x.com", "twitter.com", StringComparison.OrdinalIgnoreCase)
                    : value
                : type switch
                {
                    ContactMethodType.Twitter => $"https://twitter.com/{value.TrimStart('@')}",
                    ContactMethodType.Facebook => $"https://facebook.com/{value}",
                    ContactMethodType.Instagram => $"https://instagram.com/{value.TrimStart('@')}",
                    ContactMethodType.LinkedIn => $"https://linkedin.com/in/{value}",
                    ContactMethodType.GitHub => $"https://github.com/{value}",
                    ContactMethodType.YouTube => $"https://youtube.com/{(value.StartsWith('@') ? value : "@" + value)}",
                    ContactMethodType.Twitch => $"https://twitch.tv/{value}",
                    ContactMethodType.TikTok => $"https://tiktok.com/{(value.StartsWith('@') ? value : "@" + value)}",
                    ContactMethodType.Telegram => $"https://t.me/{value.TrimStart('@')}",
                    ContactMethodType.WhatsApp => $"https://wa.me/{value}",
                    ContactMethodType.Skype => $"skype:{value}?chat",
                    _ => value
                };
        }
    }
}