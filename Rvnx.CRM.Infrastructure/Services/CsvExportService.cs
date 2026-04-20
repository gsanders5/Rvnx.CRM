using System.Globalization;
using System.Text;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Services;

public sealed record CsvColumn(string Header, Func<Contact, CsvExportContext, string> GetValue);

public sealed class CsvExportContext
{
    public IReadOnlyDictionary<Guid, List<string>> EmailsByContact { get; init; } =
        new Dictionary<Guid, List<string>>();
    public IReadOnlyDictionary<Guid, List<string>> PhonesByContact { get; init; } =
        new Dictionary<Guid, List<string>>();
    public IReadOnlyDictionary<Guid, Address> FirstAddressByContact { get; init; } =
        new Dictionary<Guid, Address>();
    public IReadOnlyDictionary<Guid, DateOnly> BirthdayByContact { get; init; } =
        new Dictionary<Guid, DateOnly>();
}

public class CsvExportService(IRepository repository) : ICsvExportService
{
    private readonly IRepository _repository = repository;

    public const int MaxEmails = 5;
    public const int MaxPhones = 5;

    public static IReadOnlyList<CsvColumn> Columns { get; } = BuildColumns();

    public async Task<ContactExportResult> ExportContactsAsync()
    {
        List<Contact> contacts = await _repository.ListAsNoTrackingAsync<Contact>(c => !c.IsHidden && !c.IsPartial);

        List<Guid> contactIds = [.. contacts.Select(c => c.Id)];

        List<ContactMethod> methods = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<ContactMethod, ContactMethod, Guid>(
                contactIds,
                chunk => cm => cm.ContactId.HasValue && chunk.Contains(cm.ContactId.Value),
                cm => new ContactMethod
                {
                    Id = cm.Id,
                    ContactId = cm.ContactId,
                    Type = cm.Type,
                    Value = cm.Value,
                    Label = cm.Label,
                    CreatedDate = cm.CreatedDate
                })
            : [];

        Dictionary<Guid, List<string>> emailsByContact = [];
        Dictionary<Guid, List<string>> phonesByContact = [];
        foreach (ContactMethod cm in methods.OrderBy(m => m.CreatedDate))
        {
            if (!cm.ContactId.HasValue)
            {
                continue;
            }

            if (cm.Type == ContactMethodType.Email)
            {
                if (!emailsByContact.TryGetValue(cm.ContactId.Value, out List<string>? list))
                {
                    list = [];
                    emailsByContact[cm.ContactId.Value] = list;
                }
                list.Add(cm.Value);
            }
            else if (cm.Type == ContactMethodType.Phone)
            {
                if (!phonesByContact.TryGetValue(cm.ContactId.Value, out List<string>? list))
                {
                    list = [];
                    phonesByContact[cm.ContactId.Value] = list;
                }
                list.Add(cm.Value);
            }
        }

        List<Address> addresses = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<Address, Address, Guid>(
                contactIds,
                chunk => a => a.ContactId.HasValue && chunk.Contains(a.ContactId.Value),
                a => new Address
                {
                    Id = a.Id,
                    ContactId = a.ContactId,
                    Line1 = a.Line1,
                    Line2 = a.Line2,
                    City = a.City,
                    State = a.State,
                    Zip = a.Zip,
                    Country = a.Country,
                    AddressType = a.AddressType,
                    CreatedDate = a.CreatedDate
                })
            : [];

        Dictionary<Guid, Address> firstAddressByContact = [];
        foreach (Address a in addresses.OrderBy(a => a.CreatedDate))
        {
            if (!a.ContactId.HasValue)
            {
                continue;
            }

            firstAddressByContact.TryAdd(a.ContactId.Value, a);
        }

        List<(Guid ContactId, DateOnly EventDate)> birthdayDates = contactIds.Count > 0
            ? await _repository.ListProjectedByChunkedContainsAsync<SignificantDate, (Guid, DateOnly), Guid>(
                contactIds,
                chunk => sd => sd.ContactId.HasValue && chunk.Contains(sd.ContactId.Value) &&
                    sd.Title == SignificantDateTitles.Birthday,
                sd => new ValueTuple<Guid, DateOnly>(sd.ContactId!.Value, sd.EventDate))
            : [];

        Dictionary<Guid, DateOnly> birthdayByContact = new(birthdayDates.Count);
        foreach (var (cid, date) in birthdayDates)
        {
            birthdayByContact.TryAdd(cid, date);
        }

        CsvExportContext context = new()
        {
            EmailsByContact = emailsByContact,
            PhonesByContact = phonesByContact,
            FirstAddressByContact = firstAddressByContact,
            BirthdayByContact = birthdayByContact
        };

        byte[] bytes = BuildCsvBytes(contacts, context);
        string fileName = $"contacts_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return new ContactExportResult
        {
            FileContent = bytes,
            FileName = fileName,
            ContentType = "text/csv"
        };
    }

    private static byte[] BuildCsvBytes(IEnumerable<Contact> contacts, CsvExportContext context)
    {
        StringBuilder sb = new();
        sb.Append(string.Join(",", Columns.Select(c => EscapeField(c.Header))));
        sb.Append("\r\n");

        foreach (Contact contact in contacts)
        {
            for (int i = 0; i < Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                string value = Columns[i].GetValue(contact, context) ?? string.Empty;
                sb.Append(EscapeField(value));
            }
            sb.Append("\r\n");
        }

        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] result = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(body, 0, result, bom.Length, body.Length);
        return result;
    }

    private static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool needsQuoting = value.IndexOfAny(['"', ',', '\r', '\n']) >= 0;
        if (!needsQuoting)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<CsvColumn> BuildColumns()
    {
        List<CsvColumn> columns =
        [
            new("contact_id", (c, _) => c.Id.ToString()),
            new("first_name", (c, _) => c.FirstName ?? string.Empty),
            new("last_name", (c, _) => c.LastName ?? string.Empty),
            new("nickname", (c, _) => c.Nickname ?? string.Empty),
            new("company", (c, _) => c.Company ?? string.Empty),
            new("job_title", (c, _) => c.JobTitle ?? string.Empty),
            new("pronouns", (c, _) => c.Pronouns ?? string.Empty),
            new("gender", (c, _) => c.Gender ?? string.Empty),
            new("religion", (c, _) => c.Religion ?? string.Empty),
            new("birthday", (c, ctx) => ctx.BirthdayByContact.TryGetValue(c.Id, out DateOnly d)
                ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty),
            new("is_hidden", (c, _) => c.IsHidden ? "true" : "false"),
            new("is_partial", (c, _) => c.IsPartial ? "true" : "false"),
        ];

        for (int i = 1; i <= MaxEmails; i++)
        {
            int index = i;
            columns.Add(new CsvColumn($"email_{index}", (c, ctx) => GetItemAt(ctx.EmailsByContact, c.Id, index - 1)));
        }

        for (int i = 1; i <= MaxPhones; i++)
        {
            int index = i;
            columns.Add(new CsvColumn($"phone_{index}", (c, ctx) => GetItemAt(ctx.PhonesByContact, c.Id, index - 1)));
        }

        columns.Add(new CsvColumn("address_1_line1", (c, ctx) => GetAddress(ctx, c.Id)?.Line1 ?? string.Empty));
        columns.Add(new CsvColumn("address_1_line2", (c, ctx) => GetAddress(ctx, c.Id)?.Line2 ?? string.Empty));
        columns.Add(new CsvColumn("address_1_city", (c, ctx) => GetAddress(ctx, c.Id)?.City ?? string.Empty));
        columns.Add(new CsvColumn("address_1_state", (c, ctx) => GetAddress(ctx, c.Id)?.State ?? string.Empty));
        columns.Add(new CsvColumn("address_1_zip", (c, ctx) => GetAddress(ctx, c.Id)?.Zip ?? string.Empty));
        columns.Add(new CsvColumn("address_1_country", (c, ctx) => GetAddress(ctx, c.Id)?.Country ?? string.Empty));
        columns.Add(new CsvColumn("address_1_type", (c, ctx) => GetAddress(ctx, c.Id)?.AddressType ?? string.Empty));

        return columns;
    }

    private static string GetItemAt(IReadOnlyDictionary<Guid, List<string>> map, Guid id, int index)
    {
        if (!map.TryGetValue(id, out List<string>? list))
        {
            return string.Empty;
        }

        return index < list.Count ? list[index] : string.Empty;
    }

    private static Address? GetAddress(CsvExportContext ctx, Guid id)
    {
        return ctx.FirstAddressByContact.TryGetValue(id, out Address? a) ? a : null;
    }
}
