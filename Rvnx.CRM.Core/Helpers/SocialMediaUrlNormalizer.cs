using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.Helpers
{
    public static class SocialMediaUrlNormalizer
    {
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