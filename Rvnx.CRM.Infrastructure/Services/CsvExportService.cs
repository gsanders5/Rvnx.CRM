using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Globalization;
using System.Text;

namespace Rvnx.CRM.Infrastructure.Services;

public class CsvExportService(IRepository repository) : ICsvExportService
{
    private const int MaxEmails = 5;
    private const int MaxPhones = 5;

    public static IReadOnlyList<string> ColumnHeaders { get; } = BuildColumnHeaders();

    public async Task<ContactExportResult> ExportContactsAsync()
    {
        List<Contact> contacts = await repository.ListAsNoTrackingAsync<Contact>(c => !c.IsPartial);
        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        (Dictionary<Guid, List<string>> emails, Dictionary<Guid, List<string>> phones) = await LoadContactMethodsAsync(contactIds);
        Dictionary<Guid, Address> firstAddress = await LoadFirstAddressPerContactAsync(contactIds);
        Dictionary<Guid, DateOnly> birthdays = await LoadBirthdaysAsync(contactIds);

        byte[] bytes = BuildCsvBytes(contacts, emails, phones, firstAddress, birthdays);

        return new ContactExportResult
        {
            FileContent = bytes,
            FileName = $"contacts_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv"
        };
    }

    private async Task<(Dictionary<Guid, List<string>> Emails, Dictionary<Guid, List<string>> Phones)> LoadContactMethodsAsync(
        List<Guid> contactIds)
    {
        Dictionary<Guid, List<string>> emails = [];
        Dictionary<Guid, List<string>> phones = [];

        if (contactIds.Count == 0)
        {
            return (emails, phones);
        }

        List<(Guid ContactId, ContactMethodType Type, string Value, DateTime CreatedDate)> rows =
            await repository.ListProjectedByChunkedContainsAsync<ContactMethod, (Guid, ContactMethodType, string, DateTime), Guid>(
                contactIds,
                chunk => cm => cm.ContactId.HasValue && chunk.Contains(cm.ContactId.Value),
                cm => new ValueTuple<Guid, ContactMethodType, string, DateTime>(
                    cm.ContactId!.Value, cm.Type, cm.Value, cm.CreatedDate));

        foreach ((Guid contactId, ContactMethodType type, string value, DateTime _) in rows.OrderBy(r => r.CreatedDate))
        {
            Dictionary<Guid, List<string>>? bucket = type switch
            {
                ContactMethodType.Email => emails,
                ContactMethodType.Phone => phones,
                _ => null
            };

            if (bucket == null)
            {
                continue;
            }

            if (!bucket.TryGetValue(contactId, out List<string>? list))
            {
                list = [];
                bucket[contactId] = list;
            }
            list.Add(value);
        }

        return (emails, phones);
    }

    private async Task<Dictionary<Guid, Address>> LoadFirstAddressPerContactAsync(List<Guid> contactIds)
    {
        Dictionary<Guid, Address> firstAddress = [];
        if (contactIds.Count == 0)
        {
            return firstAddress;
        }

        List<Address> addresses = await repository.ListProjectedByChunkedContainsAsync<Address, Address, Guid>(
            contactIds,
            chunk => a => a.ContactId.HasValue && chunk.Contains(a.ContactId.Value),
            a => new Address
            {
                ContactId = a.ContactId,
                Line1 = a.Line1,
                Line2 = a.Line2,
                City = a.City,
                State = a.State,
                Zip = a.Zip,
                Country = a.Country,
                AddressType = a.AddressType,
                CreatedDate = a.CreatedDate
            });

        foreach (Address a in addresses.OrderBy(a => a.CreatedDate))
        {
            firstAddress.TryAdd(a.ContactId!.Value, a);
        }

        return firstAddress;
    }

    private async Task<Dictionary<Guid, DateOnly>> LoadBirthdaysAsync(List<Guid> contactIds)
    {
        Dictionary<Guid, DateOnly> birthdays = [];
        if (contactIds.Count == 0)
        {
            return birthdays;
        }

        List<(Guid ContactId, DateOnly EventDate)> rows =
            await repository.ListProjectedByChunkedContainsAsync<SignificantDate, (Guid, DateOnly), Guid>(
                contactIds,
                chunk => sd => sd.ContactId.HasValue && chunk.Contains(sd.ContactId.Value)
                    && sd.Title == SignificantDateTitles.Birthday,
                sd => new ValueTuple<Guid, DateOnly>(sd.ContactId!.Value, sd.EventDate));

        foreach ((Guid cid, DateOnly date) in rows)
        {
            birthdays.TryAdd(cid, date);
        }

        return birthdays;
    }

    private static byte[] BuildCsvBytes(
        IEnumerable<Contact> contacts,
        Dictionary<Guid, List<string>> emails,
        Dictionary<Guid, List<string>> phones,
        Dictionary<Guid, Address> firstAddress,
        Dictionary<Guid, DateOnly> birthdays)
    {
        using MemoryStream stream = new();
        stream.Write(Encoding.UTF8.GetPreamble());

        using (StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(string.Join(",", ColumnHeaders));

            foreach (Contact c in contacts)
            {
                emails.TryGetValue(c.Id, out List<string>? emailList);
                phones.TryGetValue(c.Id, out List<string>? phoneList);
                firstAddress.TryGetValue(c.Id, out Address? addr);
                string birthday = birthdays.TryGetValue(c.Id, out DateOnly d)
                    ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : string.Empty;

                string[] fields =
                [
                    c.Id.ToString(),
                    c.FirstName ?? string.Empty,
                    c.LastName ?? string.Empty,
                    c.Nickname ?? string.Empty,
                    c.Company ?? string.Empty,
                    c.JobTitle ?? string.Empty,
                    c.Pronouns ?? string.Empty,
                    c.Gender ?? string.Empty,
                    c.Religion ?? string.Empty,
                    birthday,
                    c.IsHidden ? "true" : "false",
                    c.IsPartial ? "true" : "false",
                    ItemAt(emailList, 0), ItemAt(emailList, 1), ItemAt(emailList, 2), ItemAt(emailList, 3), ItemAt(emailList, 4),
                    ItemAt(phoneList, 0), ItemAt(phoneList, 1), ItemAt(phoneList, 2), ItemAt(phoneList, 3), ItemAt(phoneList, 4),
                    addr?.Line1 ?? string.Empty,
                    addr?.Line2 ?? string.Empty,
                    addr?.City ?? string.Empty,
                    addr?.State ?? string.Empty,
                    addr?.Zip ?? string.Empty,
                    addr?.Country ?? string.Empty,
                    addr?.AddressType ?? string.Empty,
                ];

                writer.WriteLine(string.Join(",", fields.Select(EscapeField)));
            }
        }

        return stream.ToArray();
    }

    private static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOfAny(['"', ',', '\r', '\n']) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string ItemAt(List<string>? list, int index)
    {
        return list != null && index < list.Count ? list[index] : string.Empty;
    }

    private static List<string> BuildColumnHeaders()
    {
        List<string> headers =
        [
            "contact_id", "first_name", "last_name", "nickname", "company",
            "job_title", "pronouns", "gender", "religion", "birthday",
            "is_hidden", "is_partial",
        ];

        for (int i = 1; i <= MaxEmails; i++)
        {
            headers.Add($"email_{i}");
        }
        for (int i = 1; i <= MaxPhones; i++)
        {
            headers.Add($"phone_{i}");
        }

        headers.AddRange(
        [
            "address_1_line1", "address_1_line2", "address_1_city",
            "address_1_state", "address_1_zip", "address_1_country", "address_1_type"
        ]);

        return headers;
    }
}
