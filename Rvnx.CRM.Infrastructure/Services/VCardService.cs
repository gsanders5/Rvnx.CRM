using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using FolkerKinzel.VCards.Extensions;
using FolkerKinzel.VCards.Models;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Helpers;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace Rvnx.CRM.Infrastructure.Services;

public class VCardService : IVCardService
{
    private const string MaidenNameKey = "X-MAIDENNAME";
    private const string GenderKey = "X-GENDER";

    private readonly HttpClient? _httpClient;
    private readonly ILogger<VCardService>? _logger;

    private static readonly Action<ILogger, Uri, Exception?> LogWarningDownloadingPhoto =
        LoggerMessage.Define<Uri>(
            LogLevel.Warning,
            new EventId(1, nameof(LogWarningDownloadingPhoto)),
            "Error downloading photo from {PhotoUri}");

    private static readonly Action<ILogger, string, Exception?> LogWarningInvalidPhoneOnImport =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogWarningInvalidPhoneOnImport)),
            "vCard import: skipping unparseable phone number {Phone}");

    public VCardService(HttpClient? httpClient = null, ILogger<VCardService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Contact>> ParseVCardAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        // v8: Use Vcf.Deserialize with Stream directly, leaveStreamOpen to not dispose the caller's stream
        IReadOnlyList<VCard> vCards = Vcf.Deserialize(fileStream, leaveStreamOpen: true);

        List<Task<Contact>> tasks = new(vCards.Count);
        foreach (VCard vc in vCards)
        {
            tasks.Add(MapVCardToContactAsync(vc, cancellationToken));
        }

        Contact[] contacts = await Task.WhenAll(tasks);
        return contacts;
    }

    private async Task<Contact> MapVCardToContactAsync(VCard vc, CancellationToken cancellationToken)
    {
        Contact contact = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "",
            LastName = ""
        };

        FolkerKinzel.VCards.Models.Properties.NameProperty? nameProp = vc.NameViews?.FirstOrNull();
        if (nameProp?.Value is Name n)
        {
            // v8: Name uses Surnames and Given (both IReadOnlyList<string>)
            contact.LastName = n.Surnames.Count > 0 ? n.Surnames[0] : "";
            contact.FirstName = n.Given.Count > 0 ? n.Given[0] : "";
        }

        FolkerKinzel.VCards.Models.Properties.NonStandardProperty? maidenNameProp =
            vc.NonStandards?.FirstOrDefault(p =>
                p is not null && MaidenNameKey.Equals(p.Key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(maidenNameProp?.Value))
        {
            contact.MaidenName = maidenNameProp.Value;
        }

        if (string.IsNullOrEmpty(contact.FirstName) && string.IsNullOrEmpty(contact.LastName))
        {
            FolkerKinzel.VCards.Models.Properties.TextProperty? displayProp = vc.DisplayNames?.FirstOrNull();
            if (displayProp != null && !string.IsNullOrWhiteSpace(displayProp.Value))
            {
                string displayName = displayProp.Value;
                if (displayName.Contains(','))
                {
                    string[] parts = displayName.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        contact.LastName = parts[0].Trim();
                    }

                    if (parts.Length > 1)
                    {
                        contact.FirstName = parts[1].Trim();
                    }
                }
                else
                {
                    string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        contact.FirstName = parts[0];
                    }

                    if (parts.Length > 1)
                    {
                        contact.LastName = string.Join(" ", parts.Skip(1));
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(contact.FirstName) && !string.IsNullOrEmpty(contact.LastName))
        {
            contact.FirstName = contact.LastName;
            contact.LastName = "";
        }
        else if (string.IsNullOrEmpty(contact.FirstName))
        {
            contact.FirstName = "Unknown";
        }

        FolkerKinzel.VCards.Models.Properties.StringCollectionProperty? nicknameProp = vc.NickNames?.FirstOrNull();
        if (nicknameProp?.Value is IReadOnlyList<string> nicknames && nicknames.Count > 0)
        {
            contact.Nickname = nicknames[0];
        }

        FolkerKinzel.VCards.Models.Properties.OrgProperty? orgProp = vc.Organizations?.FirstOrNull();
        if (orgProp?.Value is Organization org)
        {
            contact.Company = org.Name;
        }

        FolkerKinzel.VCards.Models.Properties.TextProperty? titleProp = vc.Titles?.FirstOrNull();
        if (titleProp != null)
        {
            contact.JobTitle = titleProp.Value;
        }

        if (vc.EMails != null)
        {
            foreach (FolkerKinzel.VCards.Models.Properties.TextProperty? emailProp in vc.EMails)
            {
                if (emailProp?.Value is string email && !string.IsNullOrWhiteSpace(email))
                {
                    contact.ContactMethods.Add(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        ContactId = contact.Id,
                        Type = ContactMethodType.Email,
                        Value = email,
                        Label = "Imported"
                    });
                }
            }
        }

        if (vc.Phones != null)
        {
            foreach (FolkerKinzel.VCards.Models.Properties.TextProperty? phoneProp in vc.Phones)
            {
                if (phoneProp?.Value is string phone && !string.IsNullOrWhiteSpace(phone))
                {
                    if (!PhoneNumberNormalizer.TryNormalize(phone, out string normalizedPhone, out _))
                    {
                        if (_logger != null)
                        {
                            LogWarningInvalidPhoneOnImport(_logger, phone, null);
                        }
                        continue;
                    }

                    contact.ContactMethods.Add(new ContactMethod
                    {
                        Id = Guid.NewGuid(),
                        ContactId = contact.Id,
                        Type = ContactMethodType.Phone,
                        Value = normalizedPhone,
                        Label = "Imported"
                    });
                }
            }
        }

        if (vc.BirthDayViews != null)
        {
            FolkerKinzel.VCards.Models.Properties.DateAndOrTimeProperty? bdayProp = vc.BirthDayViews.FirstOrNull();
            if (bdayProp?.Value is DateAndOrTime val)
            {
                SignificantDate? bday = null;
                if (val.DateTimeOffset.HasValue)
                {
                    bday = new SignificantDate
                    {
                        Id = Guid.NewGuid(),
                        ContactId = contact.Id,
                        Title = SignificantDateTitles.Birthday,
                        EventDate = DateOnly.FromDateTime(val.DateTimeOffset.Value.DateTime),
                        Description = "Birthday from VCard",
                        RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                        IsActive = true
                    };
                }
                else if (val.DateOnly.HasValue)
                {
                    bday = new SignificantDate
                    {
                        Id = Guid.NewGuid(),
                        ContactId = contact.Id,
                        Title = SignificantDateTitles.Birthday,
                        EventDate = val.DateOnly.Value,
                        Description = "Birthday from VCard",
                        RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                        IsActive = true
                    };
                }

                if (bday != null)
                {
                    bday.ReminderOffsets.Add(new ReminderOffset
                    {
                        Id = Guid.NewGuid(),
                        SignificantDateId = bday.Id,
                        DaysBeforeEvent = 0,
                        IsActive = true
                    });
                    contact.SignificantDates.Add(bday);
                }
            }
        }

        // Gender - try standard GenderViews first (v4.0 files), then fall back to
        // X-GENDER non-standard property (v3.0 files exported by this app and others)
        FolkerKinzel.VCards.Models.Properties.GenderProperty? genderProp = vc.GenderViews?.FirstOrNull();
        if (genderProp?.Value is Gender gender)
        {
            contact.Gender = gender.Sex switch
            {
                Sex.Male => PersonalAttributeOptions.Male,
                Sex.Female => PersonalAttributeOptions.Female,
                Sex.Other => PersonalAttributeOptions.Other,
                _ => null
            };
        }
        else
        {
            // X-GENDER is the widely-used v3.0 extension for gender
            FolkerKinzel.VCards.Models.Properties.NonStandardProperty? xGenderProp =
                vc.NonStandards?.FirstOrDefault(p =>
                    p is not null && GenderKey.Equals(p.Key, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(xGenderProp?.Value))
            {
                contact.Gender = xGenderProp.Value;
            }
        }

        (byte[]? photoBytes, string? mediaType) = await TryGetPhotoAsync(vc, _httpClient != null, cancellationToken);
        if (photoBytes != null && photoBytes.Length > 0)
        {
            string extension = ".jpg";
            string contentType = "image/jpeg";

            // Refine using MediaType if available
            if (!string.IsNullOrEmpty(mediaType))
            {
                if (mediaType.Contains("png", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".png";
                    contentType = "image/png";
                }
                else if (mediaType.Contains("gif", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".gif";
                    contentType = "image/gif";
                }
                else if (mediaType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || mediaType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".jpg";
                    contentType = "image/jpeg";
                }
            }

            Attachment attachment = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contact.Id,
                AttachmentType = AttachmentTypes.ProfileImage,
                ContentType = contentType,
                FileName = $"vcard_photo{extension}"
            };

            AttachmentContent content = new()
            {
                Id = Guid.NewGuid(),
                AttachmentId = attachment.Id,
                Content = photoBytes
            };

            attachment.AttachmentContent = content;
            contact.Attachments.Add(attachment);
        }

        return contact;
    }

    private async Task<(byte[]? Content, string? MediaType)> TryGetPhotoAsync(VCard vc, bool resolveUrls, CancellationToken cancellationToken)
    {
        FolkerKinzel.VCards.Models.Properties.DataProperty? photoProp = vc.Photos?.FirstOrNull();
        if (photoProp?.Value is not RawData rawData)
        {
            return (null, null);
        }

        byte[]? bytes = rawData.Bytes;
        if (bytes != null && bytes.Length > 0)
        {
            return (bytes, photoProp.Parameters.MediaType);
        }

        Uri? photoUri = rawData.Uri;
        if (photoUri != null && resolveUrls && _httpClient != null)
        {
            try
            {
                if (photoUri.Scheme == Uri.UriSchemeHttp || photoUri.Scheme == Uri.UriSchemeHttps)
                {
                    Uri targetUri = photoUri;

                    if (!await IsSafeUriAsync(targetUri))
                    {
                        return (null, null);
                    }

                    using HttpResponseMessage response = await _httpClient.GetAsync(targetUri, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
                        return (content, contentType?.MediaType ?? photoProp.Parameters.MediaType);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    LogWarningDownloadingPhoto(_logger, photoUri, ex);
                }
            }
        }

        return (null, null);
    }

    private static async Task<bool> IsSafeUriAsync(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out IPAddress? ipAddress))
        {
            return IsPublicIpAddress(ipAddress);
        }

        try
        {
            IPAddress[] ips = await Dns.GetHostAddressesAsync(uri.Host);
            return ips.All(IsPublicIpAddress);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPublicIpAddress(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return false;
        }

        IPAddress ipv6 = ipAddress.MapToIPv6();
        byte[] bytes = ipv6.GetAddressBytes();

        if (bytes.All(b => b == 0))
        {
            return false;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            if (ipAddress.Equals(IPAddress.Any))
            {
                return false;
            }

            byte[] v4bytes = ipAddress.GetAddressBytes();

            return v4bytes[0] != 0 && v4bytes[0] != 10 && (v4bytes[0] != 172 || v4bytes[1] < 16 || v4bytes[1] > 31) && (v4bytes[0] != 192 || v4bytes[1] != 168) && (v4bytes[0] != 169 || v4bytes[1] != 254) && v4bytes[0] != 127;
        }
        else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ipAddress.IsIPv4MappedToIPv6)
            {
                IPAddress v4 = ipAddress.MapToIPv4();
                return IsPublicIpAddress(v4);
            }

            return (bytes[0] != 0xFE || (bytes[1] & 0xC0) != 0x80) &&
                   (bytes[0] & 0xFE) != 0xFC &&
                   (bytes[0] != 0x20 || bytes[1] != 0x01 || bytes[2] != 0x0D || bytes[3] != 0xB8);
        }

        return false;
    }

    public byte[] ExportVCard(Contact contact)
    {
        VCard vCard = VCardBuilder
            .Create()
            .NameViews.Add(NameBuilder
                .Create()
                .AddSurname(contact.LastName ?? "")
                .AddGiven(contact.FirstName)
                .Build())
            .DisplayNames.Add(contact.FullName)
            .VCard;

        VCardBuilder builder = VCardBuilder.Create(vCard);

        if (!string.IsNullOrEmpty(contact.Company))
        {
            builder.Organizations.Add(contact.Company);
        }

        if (!string.IsNullOrEmpty(contact.JobTitle))
        {
            builder.Titles.Add(contact.JobTitle);
        }

        if (contact.ContactMethods != null)
        {
            foreach (ContactMethod? cm in contact.ContactMethods.Where(m => m.Type == ContactMethodType.Email && !string.IsNullOrEmpty(m.Value)))
            {
                builder.EMails.Add(cm.Value);
            }
        }

        if (contact.ContactMethods != null)
        {
            foreach (ContactMethod? cm in contact.ContactMethods.Where(m => m.Type == ContactMethodType.Phone && !string.IsNullOrEmpty(m.Value)))
            {
                builder.Phones.Add(cm.Value);
            }
        }

        if (contact.SignificantDates != null)
        {
            SignificantDate? bday = contact.SignificantDates.FirstOrDefault(d => d.Title == SignificantDateTitles.Birthday);
            if (bday != null)
            {
                builder.BirthDayViews.Add(bday.EventDate.Year, bday.EventDate.Month, bday.EventDate.Day);
            }
        }

        if (contact.Attachments != null)
        {
            Attachment? profileImage = contact.Attachments.FirstOrDefault(a => a.AttachmentType == AttachmentTypes.ProfileImage);
            if (profileImage != null && profileImage.AttachmentContent != null && profileImage.AttachmentContent.Content.Length > 0)
            {
                builder.Photos.AddBytes(profileImage.AttachmentContent.Content, profileImage.ContentType ?? "image/jpeg");
            }
        }

        vCard = builder.VCard;

        string vcfString = vCard.ToVcfString(VCdVersion.V3_0);

        // Fix compatibility: Replace "ENCODING=b" with "ENCODING=BASE64"
        // The library uses the abbreviated form per RFC 2426, but many applications
        // (including Google Contacts) only recognize the full "BASE64" form
        vcfString = vcfString.Replace("ENCODING=b;", "ENCODING=BASE64;")
                             .Replace("ENCODING=b:", "ENCODING=BASE64:");

        // X-MAIDENNAME and X-GENDER have no standard vCard 3.0 fields and are silently
        // dropped by FolkerKinzel during v3.0 serialization. Inject them manually before
        // END:VCARD so they round-trip correctly through import/export.
        System.Text.StringBuilder extensions = new();

        if (!string.IsNullOrEmpty(contact.MaidenName))
        {
            extensions.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{MaidenNameKey}:{contact.MaidenName}");
        }

        if (!string.IsNullOrEmpty(contact.Gender))
        {
            extensions.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{GenderKey}:{contact.Gender}");
        }

        if (extensions.Length > 0)
        {
            vcfString = vcfString.Replace("END:VCARD", extensions + "END:VCARD");
        }

        return System.Text.Encoding.UTF8.GetBytes(vcfString);
    }
}
