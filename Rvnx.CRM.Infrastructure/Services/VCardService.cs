using FolkerKinzel.VCards;
using FolkerKinzel.VCards.Enums;
using FolkerKinzel.VCards.Extensions;
using FolkerKinzel.VCards.Models;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Services;

public class VCardService : IVCardService
{
    public IEnumerable<Contact> ParseVCard(Stream fileStream)
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

            contacts.Add(contact);
        }

        return contacts;
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

        vCard = builder.VCard;

        // v8: Use ToVcfString extension method for serialization
        return System.Text.Encoding.UTF8.GetBytes(vCard.ToVcfString(VCdVersion.V3_0));
    }
}