using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Models;
using FolkerKinzel.VCards.Models.PropertyParts;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Text;

namespace Rvnx.CRM.Infrastructure.Services;

public class VCardService : IVCardService
{
    public IEnumerable<Contact> ParseVCard(Stream fileStream)
    {
        IList<VCard> vCards;
        try
        {
            using StreamReader reader = new StreamReader(fileStream);
            vCards = VCard.DeserializeVcf(reader);
        }
        catch
        {
            return Enumerable.Empty<Contact>();
        }

        List<Contact> contacts = new();

        foreach (VCard vc in vCards)
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "",
                LastName = ""
            };

            // 1. Name
            var nameProp = vc.NameViews?.FirstOrDefault();
            if (nameProp != null && nameProp.Value != null)
            {
                Name n = nameProp.Value;
                contact.LastName = n.LastName.FirstOrDefault() ?? "";
                contact.FirstName = n.FirstName.FirstOrDefault() ?? "";
            }

            // Name Fallback using Display Name
            if (string.IsNullOrEmpty(contact.FirstName) && string.IsNullOrEmpty(contact.LastName))
            {
                var displayProp = vc.DisplayNames?.FirstOrDefault();
                if (displayProp != null && !string.IsNullOrWhiteSpace(displayProp.Value))
                {
                    string displayName = displayProp.Value;
                    if (displayName.Contains(","))
                    {
                        string[] parts = displayName.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) contact.LastName = parts[0].Trim();
                        if (parts.Length > 1) contact.FirstName = parts[1].Trim();
                    }
                    else
                    {
                        string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) contact.FirstName = parts[0];
                        if (parts.Length > 1) contact.LastName = string.Join(" ", parts.Skip(1));
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
            var nicknameProp = vc.NickNames?.FirstOrDefault();
            if (nicknameProp != null)
            {
                if (nicknameProp.Value is IEnumerable<string> list)
                {
                    contact.Nickname = list.FirstOrDefault();
                }
            }

            // Org
            var orgProp = vc.Organizations?.FirstOrDefault();
            if (orgProp != null && orgProp.Value != null)
            {
                Organization org = orgProp.Value;
                contact.Company = org.OrganizationName;
            }

            // Title
            var titleProp = vc.Titles?.FirstOrDefault();
            if (titleProp != null)
            {
                contact.JobTitle = titleProp.Value;
            }

            // Emails
            if (vc.EMails != null)
            {
                foreach (var emailProp in vc.EMails)
                {
                    if (emailProp?.Value is string email && !string.IsNullOrWhiteSpace(email))
                    {
                        contact.ContactMethods.Add(new ContactMethod
                        {
                            Id = Guid.NewGuid(),
                            EntityId = contact.Id,
                            EntityType = EntityTypes.Person,
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
                foreach (var phoneProp in vc.Phones)
                {
                    if (phoneProp?.Value is string phone && !string.IsNullOrWhiteSpace(phone))
                    {
                        contact.ContactMethods.Add(new ContactMethod
                        {
                            Id = Guid.NewGuid(),
                            EntityId = contact.Id,
                            EntityType = EntityTypes.Person,
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
                var bdayProp = vc.BirthDayViews.FirstOrDefault();
                if (bdayProp != null && bdayProp.Value != null)
                {
                     DateAndOrTime val = bdayProp.Value;
                     if (val.DateTimeOffset.HasValue)
                     {
                         contact.SignificantDates.Add(new SignificantDate
                         {
                            Id = Guid.NewGuid(),
                            EntityId = contact.Id,
                            EntityType = EntityTypes.Person,
                            Title = "Birthday",
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
                            EntityId = contact.Id,
                            EntityType = EntityTypes.Person,
                            Title = "Birthday",
                            Date = d.ToDateTime(TimeOnly.MinValue),
                            Description = "Birthday from VCard",
                            RemindMe = true,
                            EventFrequency = TimeSpan.FromDays(365)
                         });
                     }
                }
            }

            contacts.Add(contact);
        }

        return contacts;
    }

    public byte[] ExportVCard(Contact contact)
    {
        VCard vc = new VCard();

        // Name
        vc.NameViews = new []
        {
            new NameProperty(
                lastName: new [] { contact.LastName ?? "" },
                firstName: new [] { contact.FirstName },
                middleName: null,
                prefix: null,
                suffix: null,
                group: null
            )
        };

        vc.DisplayNames = new []
        {
            new TextProperty(contact.FullName)
        };

        // Org
        if (!string.IsNullOrEmpty(contact.Company))
        {
            vc.Organizations = new [] { new OrganizationProperty(contact.Company, null, null) };
        }

        // Title
        if (!string.IsNullOrEmpty(contact.JobTitle))
        {
            vc.Titles = new [] { new TextProperty(contact.JobTitle) };
        }

        // Emails
        if (contact.ContactMethods != null)
        {
            var emails = contact.ContactMethods
                .Where(m => m.Type == ContactMethodType.Email && !string.IsNullOrEmpty(m.Value))
                .Select(m => new TextProperty(m.Value))
                .ToList();

            if (emails.Any()) vc.EMails = emails;
        }

        // Phones
        if (contact.ContactMethods != null)
        {
            var phones = contact.ContactMethods
                .Where(m => m.Type == ContactMethodType.Phone && !string.IsNullOrEmpty(m.Value))
                .Select(m => new TextProperty(m.Value))
                .ToList();

            if (phones.Any()) vc.Phones = phones;
        }

        // Birthday
        if (contact.SignificantDates != null)
        {
            var bday = contact.SignificantDates.FirstOrDefault(d => d.Title == "Birthday");
            if (bday != null)
            {
                vc.BirthDayViews = new [] { DateAndOrTimeProperty.FromDate(DateOnly.FromDateTime(bday.Date)) };
            }
        }

        using MemoryStream ms = new();
        VCard.SerializeVcf(ms, new[] { vc }, VCdVersion.V3_0);
        return ms.ToArray();
    }
}
