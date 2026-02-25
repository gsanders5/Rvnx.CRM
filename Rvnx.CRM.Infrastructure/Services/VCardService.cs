using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using FolkerKinzel.VCards.Extensions;
using FolkerKinzel.VCards.Models;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Net.Http.Headers;

namespace Rvnx.CRM.Infrastructure.Services;

public class VCardService : IVCardService
{
    private readonly HttpClient? _httpClient;

    public VCardService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Contact>> ParseVCardAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<VCard> vCards;
        try
        {
            // v8: Use Vcf.Deserialize with Stream directly, leaveStreamOpen to not dispose the caller's stream
            vCards = Vcf.Deserialize(fileStream, leaveStreamOpen: true);
        }
        catch
        {
            return Enumerable.Empty<Contact>();
        }

        List<Contact> contacts = [];

        foreach (VCard vc in vCards)
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "",
                LastName = ""
            };

            // 1. Name - use extension method FirstOrNull() for safer access
            FolkerKinzel.VCards.Models.Properties.NameProperty? nameProp = vc.NameViews?.FirstOrNull();
            if (nameProp?.Value is Name n)
            {
                // v8: Name uses Surnames and Given (both IReadOnlyList<string>)
                contact.LastName = n.Surnames.Count > 0 ? n.Surnames[0] : "";
                contact.FirstName = n.Given.Count > 0 ? n.Given[0] : "";
            }

            // Name Fallback using Display Name
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

            // Nickname
            FolkerKinzel.VCards.Models.Properties.StringCollectionProperty? nicknameProp = vc.NickNames?.FirstOrNull();
            if (nicknameProp?.Value is IReadOnlyList<string> nicknames && nicknames.Count > 0)
            {
                contact.Nickname = nicknames[0];
            }

            // Org - v8: Organization.Name (not OrganizationName)
            FolkerKinzel.VCards.Models.Properties.OrgProperty? orgProp = vc.Organizations?.FirstOrNull();
            if (orgProp?.Value is Organization org)
            {
                contact.Company = org.Name;
            }

            // Title
            FolkerKinzel.VCards.Models.Properties.TextProperty? titleProp = vc.Titles?.FirstOrNull();
            if (titleProp != null)
            {
                contact.JobTitle = titleProp.Value;
            }

            // Emails
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

            // Phones
            if (vc.Phones != null)
            {
                foreach (FolkerKinzel.VCards.Models.Properties.TextProperty? phoneProp in vc.Phones)
                {
                    if (phoneProp?.Value is string phone && !string.IsNullOrWhiteSpace(phone))
                    {
                        contact.ContactMethods.Add(new ContactMethod
                        {
                            Id = Guid.NewGuid(),
                            ContactId = contact.Id,
                            Type = ContactMethodType.Phone,
                            Value = phone,
                            Label = "Imported"
                        });
                    }
                }
            }

            // Birthday
            if (vc.BirthDayViews != null)
            {
                FolkerKinzel.VCards.Models.Properties.DateAndOrTimeProperty? bdayProp = vc.BirthDayViews.FirstOrNull();
                if (bdayProp?.Value is DateAndOrTime val)
                {
                    if (val.DateTimeOffset.HasValue)
                    {
                        contact.SignificantDates.Add(new SignificantDate
                        {
                            Id = Guid.NewGuid(),
                            ContactId = contact.Id,
                            Title = SignificantDateTitles.Birthday,
                            Date = val.DateTimeOffset.Value.DateTime,
                            Description = "Birthday from VCard",
                            RemindMe = true,
                            EventFrequency = TimeSpan.FromDays(365)
                        });
                    }
                    else if (val.DateOnly.HasValue)
                    {
                        DateOnly d = val.DateOnly.Value;
                        contact.SignificantDates.Add(new SignificantDate
                        {
                            Id = Guid.NewGuid(),
                            ContactId = contact.Id,
                            Title = SignificantDateTitles.Birthday,
                            Date = d.ToDateTime(TimeOnly.MinValue),
                            Description = "Birthday from VCard",
                            RemindMe = true,
                            EventFrequency = TimeSpan.FromDays(365)
                        });
                    }
                }
            }

            // Gender - v8: GenderProperty.Value.Sex returns Sex enum
            FolkerKinzel.VCards.Models.Properties.GenderProperty? genderProp = vc.GenderViews?.FirstOrNull();
            if (genderProp?.Value is Gender gender)
            {
                contact.Gender = gender.Sex switch
                {
                    Sex.Male => "Male",
                    Sex.Female => "Female",
                    Sex.Other => "Other",
                    _ => null
                };
            }

            // Photo - attempt to resolve
            (byte[]? photoBytes, string? mediaType) = await TryGetPhotoAsync(vc, _httpClient != null, cancellationToken);
            if (photoBytes != null && photoBytes.Length > 0)
            {
                // Default to jpg
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

            contacts.Add(contact);
        }

        return contacts;
    }

    private async Task<(byte[]? Content, string? MediaType)> TryGetPhotoAsync(VCard vc, bool resolveUrls, CancellationToken cancellationToken)
    {
        var photoProp = vc.Photos?.FirstOrNull();
        if (photoProp?.Value is not RawData rawData)
        {
            return (null, null);
        }

        // Try to get embedded bytes first
        byte[]? bytes = rawData.Bytes;
        if (bytes != null && bytes.Length > 0)
        {
            return (bytes, photoProp.Parameters.MediaType);
        }

        // Try to resolve from URI if we have an HttpClient and URL resolution is enabled
        Uri? photoUri = rawData.Uri;
        if (photoUri != null && resolveUrls && _httpClient != null)
        {
            try
            {
                // Only fetch http/https URLs
                if (photoUri.Scheme == Uri.UriSchemeHttp || photoUri.Scheme == Uri.UriSchemeHttps)
                {
                    using HttpResponseMessage response = await _httpClient.GetAsync(photoUri, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
                        return (content, contentType?.MediaType ?? photoProp.Parameters.MediaType);
                    }
                }
            }
            catch
            {
                // Failed to fetch photo - continue without it
            }
        }

        return (null, null);
    }

    public byte[] ExportVCard(Contact contact)
    {
        // v8: Use VCardBuilder fluent API for creating VCards
        VCard vCard = VCardBuilder
            .Create()
            .NameViews.Add(NameBuilder
                .Create()
                .AddSurname(contact.LastName ?? "")
                .AddGiven(contact.FirstName)
                .Build())
            .DisplayNames.Add(contact.FullName)
            .VCard;

        // Continue building with Edit() for conditional properties
        VCardBuilder builder = VCardBuilder.Create(vCard);

        // Org
        if (!string.IsNullOrEmpty(contact.Company))
        {
            builder.Organizations.Add(contact.Company);
        }

        // Title
        if (!string.IsNullOrEmpty(contact.JobTitle))
        {
            builder.Titles.Add(contact.JobTitle);
        }

        // Emails
        if (contact.ContactMethods != null)
        {
            foreach (ContactMethod? cm in contact.ContactMethods.Where(m => m.Type == ContactMethodType.Email && !string.IsNullOrEmpty(m.Value)))
            {
                builder.EMails.Add(cm.Value);
            }
        }

        // Phones
        if (contact.ContactMethods != null)
        {
            foreach (ContactMethod? cm in contact.ContactMethods.Where(m => m.Type == ContactMethodType.Phone && !string.IsNullOrEmpty(m.Value)))
            {
                builder.Phones.Add(cm.Value);
            }
        }

        // Birthday
        if (contact.SignificantDates != null)
        {
            SignificantDate? bday = contact.SignificantDates.FirstOrDefault(d => d.Title == SignificantDateTitles.Birthday);
            if (bday != null)
            {
                builder.BirthDayViews.Add(bday.Date.Year, bday.Date.Month, bday.Date.Day);
            }
        }

        // Gender
        if (!string.IsNullOrEmpty(contact.Gender))
        {
            Sex sex = contact.Gender switch
            {
                "Male" => Sex.Male,
                "Female" => Sex.Female,
                "Non-Binary" => Sex.Other,
                _ => Sex.Other
            };
            builder.GenderViews.Add(sex);
        }

        // Photo
        if (contact.Attachments != null)
        {
            var profileImage = contact.Attachments.FirstOrDefault(a => a.AttachmentType == AttachmentTypes.ProfileImage);
            if (profileImage != null && profileImage.AttachmentContent != null && profileImage.AttachmentContent.Content.Length > 0)
            {
                builder.Photos.AddBytes(profileImage.AttachmentContent.Content, profileImage.ContentType ?? "image/jpeg");
            }
        }

        vCard = builder.VCard;

        // v8: Use ToVcfString extension method for serialization
        string vcfString = vCard.ToVcfString(VCdVersion.V3_0);

        // Fix compatibility: Replace "ENCODING=b" with "ENCODING=BASE64"
        // The library uses the abbreviated form per RFC 2426, but many applications
        // (including Google Contacts) only recognize the full "BASE64" form
        vcfString = vcfString.Replace("ENCODING=b;", "ENCODING=BASE64;")
                             .Replace("ENCODING=b:", "ENCODING=BASE64:");

        return System.Text.Encoding.UTF8.GetBytes(vcfString);
    }
}
