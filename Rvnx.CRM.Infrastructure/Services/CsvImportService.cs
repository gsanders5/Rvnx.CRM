using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Helpers;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Globalization;

namespace Rvnx.CRM.Infrastructure.Services;

public class CsvImportService(IRepository repository, ILogger<CsvImportService> logger) : ICsvImportService
{
    private readonly IRepository _repository = repository;
    private readonly ILogger<CsvImportService> _logger = logger;

    private static readonly Action<ILogger, Exception?> LogErrorImportingCsv =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogErrorImportingCsv)),
            "Error importing CSV");

    private static readonly Action<ILogger, string, Exception?> LogWarningInvalidPhoneOnImport =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogWarningInvalidPhoneOnImport)),
            "Skipping invalid phone number on CSV import: {Phone}");

    // Column indexes are resolved from the export schema by header name, so the import
    // cannot silently misalign if CsvExportService.ColumnHeaders evolves.
    // contact_id (round-trip identification) and is_partial are intentionally not imported.
    private static readonly int ColFirstName = Col("first_name");
    private static readonly int ColLastName = Col("last_name");
    private static readonly int ColNickname = Col("nickname");
    private static readonly int ColCompany = Col("company");
    private static readonly int ColJobTitle = Col("job_title");
    private static readonly int ColPronouns = Col("pronouns");
    private static readonly int ColGender = Col("gender");
    private static readonly int ColReligion = Col("religion");
    private static readonly int ColBirthday = Col("birthday");
    private static readonly int ColIsHidden = Col("is_hidden");
    private static readonly int[] EmailCols = [Col("email_1"), Col("email_2"), Col("email_3"), Col("email_4"), Col("email_5")];
    private static readonly int[] PhoneCols = [Col("phone_1"), Col("phone_2"), Col("phone_3"), Col("phone_4"), Col("phone_5")];
    private static readonly int ColAddrLine1 = Col("address_1_line1");
    private static readonly int ColAddrLine2 = Col("address_1_line2");
    private static readonly int ColAddrCity = Col("address_1_city");
    private static readonly int ColAddrState = Col("address_1_state");
    private static readonly int ColAddrZip = Col("address_1_zip");
    private static readonly int ColAddrCountry = Col("address_1_country");
    private static readonly int ColAddrType = Col("address_1_type");
    private static readonly int ExpectedColumnCount = CsvExportService.ColumnHeaders.Count;

    private static int Col(string header)
    {
        IReadOnlyList<string> headers = CsvExportService.ColumnHeaders;
        for (int i = 0; i < headers.Count; i++)
        {
            if (headers[i] == header)
            {
                return i;
            }
        }
        throw new InvalidOperationException($"CSV column '{header}' is missing from the export schema.");
    }

    public async Task<ContactImportResult> ImportFromCsvAsync(Stream csvStream)
    {
        try
        {
            int addedCount = 0;
            int skippedCount = 0;

            using StreamReader reader = new(csvStream, leaveOpen: true);

            string? headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
            {
                return new ContactImportResult { AddedCount = 0, SkippedCount = 0 };
            }

            // Validate header matches expected schema
            List<string> headerFields = ParseCsvLine(headerLine);
            IReadOnlyList<string> expectedHeaders = CsvExportService.ColumnHeaders;
            bool headerMatches = headerFields.Count == expectedHeaders.Count
                && headerFields.Zip(expectedHeaders, string.Equals).All(eq => eq);
            if (!headerMatches)
            {
                throw new FormatException("CSV header does not match the expected format.");
            }

            // Pre-load existing names and contact-method values once; checking per row
            // would cost two queries per imported contact. Rows added during this import
            // are appended to the same sets so in-file duplicates are also caught.
            HashSet<(string FirstName, string? LastName)> existingNames =
                [.. await _repository.ListProjectedAsync<Contact, (string, string?)>(
                    c => true,
                    c => new ValueTuple<string, string?>(c.FirstName, c.LastName))];
            HashSet<string> existingMethodValues =
                [.. await _repository.ListProjectedAsync<ContactMethod, string>(
                    cm => true,
                    cm => cm.Value)];

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                List<string> fields = ParseCsvLine(line);

                // Pad to expected count to guard against truncated rows
                while (fields.Count < ExpectedColumnCount)
                {
                    fields.Add(string.Empty);
                }

                string firstName = fields[ColFirstName].Trim();
                if (string.IsNullOrEmpty(firstName))
                {
                    skippedCount++;
                    continue;
                }

                Contact contact = new()
                {
                    Id = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = NullIfEmpty(fields[ColLastName]),
                    Nickname = NullIfEmpty(fields[ColNickname]),
                    Company = NullIfEmpty(fields[ColCompany]),
                    JobTitle = NullIfEmpty(fields[ColJobTitle]),
                    Pronouns = NullIfEmpty(fields[ColPronouns]),
                    Gender = NullIfEmpty(fields[ColGender]),
                    Religion = NullIfEmpty(fields[ColReligion]),
                    IsHidden = string.Equals(fields[ColIsHidden].Trim(), "true", StringComparison.OrdinalIgnoreCase),
                    IsPartial = false
                };

                List<ContactMethod> methods = BuildContactMethods(fields);

                if (existingNames.Contains((contact.FirstName, contact.LastName))
                    || methods.Any(m => existingMethodValues.Contains(m.Value)))
                {
                    skippedCount++;
                    continue;
                }

                existingNames.Add((contact.FirstName, contact.LastName));
                foreach (ContactMethod method in methods)
                {
                    existingMethodValues.Add(method.Value);
                }

                await _repository.AddAsync(contact);
                addedCount++;

                await SaveContactMethodsAsync(contact.Id, methods);
                await SaveBirthdayAsync(contact.Id, fields[ColBirthday]);
                await SaveAddressAsync(contact.Id, fields);
            }

            await _repository.SaveChangesAsync();

            return new ContactImportResult
            {
                AddedCount = addedCount,
                SkippedCount = skippedCount
            };
        }
        catch (Exception ex)
        {
            LogErrorImportingCsv(_logger, ex);
            throw;
        }
    }

    private List<ContactMethod> BuildContactMethods(List<string> fields)
    {
        List<ContactMethod> methods = [];
        AddContactMethods(methods, fields, ContactMethodType.Email, EmailCols);
        AddContactMethods(methods, fields, ContactMethodType.Phone, PhoneCols);
        return methods;
    }

    private void AddContactMethods(List<ContactMethod> methods, List<string> fields, ContactMethodType type, int[] columns)
    {
        foreach (int col in columns)
        {
            string val = fields[col].Trim();
            if (string.IsNullOrEmpty(val))
            {
                continue;
            }

            if (type == ContactMethodType.Phone)
            {
                // Mirror the vCard importer: store phones in E.164, skip values that don't parse.
                if (!PhoneNumberNormalizer.TryNormalize(val, out string normalized, out _))
                {
                    LogWarningInvalidPhoneOnImport(_logger, val, null);
                    continue;
                }
                val = normalized;
            }

            methods.Add(new ContactMethod
            {
                Id = Guid.NewGuid(),
                Type = type,
                Value = val,
                Label = ContactMethodLabels.Primary
            });
        }
    }

    private async Task SaveContactMethodsAsync(Guid contactId, List<ContactMethod> methods)
    {
        foreach (ContactMethod method in methods)
        {
            method.ContactId = contactId;
            await _repository.AddAsync(method);
        }
    }

    private async Task SaveBirthdayAsync(Guid contactId, string birthdayField)
    {
        string trimmed = birthdayField.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        if (!DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly eventDate))
        {
            return;
        }

        await _repository.AddAsync(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = SignificantDateTitles.Birthday,
            Description = "Birthday",
            EventDate = eventDate,
            RecurrenceType = RecurrenceType.Annual,
            IsActive = true
        });
    }

    private async Task SaveAddressAsync(Guid contactId, List<string> fields)
    {
        string line1 = fields[ColAddrLine1].Trim();
        if (string.IsNullOrEmpty(line1))
        {
            return;
        }

        string addressType = fields[ColAddrType].Trim();
        if (string.IsNullOrEmpty(addressType))
        {
            addressType = AddressTypes.Home;
        }

        await _repository.AddAsync(new Address
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Line1 = line1,
            Line2 = NullIfEmpty(fields[ColAddrLine2]),
            City = fields[ColAddrCity].Trim(),
            State = fields[ColAddrState].Trim(),
            Zip = fields[ColAddrZip].Trim(),
            Country = fields[ColAddrCountry].Trim(),
            AddressType = addressType
        });
    }

    /// <summary>
    /// Parses a single CSV line, correctly handling quoted fields that may contain
    /// embedded commas, newlines, and escaped double-quotes ("").
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        List<string> fields = [];
        int i = 0;
        int length = line.Length;

        while (i <= length)
        {
            if (i == length)
            {
                // Trailing comma — emit empty field
                fields.Add(string.Empty);
                break;
            }

            if (line[i] == '"')
            {
                // Quoted field
                i++; // skip opening quote
                System.Text.StringBuilder sb = new();
                while (i < length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < length && line[i + 1] == '"')
                        {
                            // Escaped quote
                            sb.Append('"');
                            i += 2;
                        }
                        else
                        {
                            // Closing quote
                            i++;
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(line[i]);
                        i++;
                    }
                }
                fields.Add(sb.ToString());
                // Skip the comma separator (or end of line)
                if (i < length && line[i] == ',')
                {
                    i++;
                }
            }
            else
            {
                // Unquoted field — read until comma or end
                int start = i;
                while (i < length && line[i] != ',')
                {
                    i++;
                }
                fields.Add(line[start..i]);
                if (i < length)
                {
                    i++; // skip comma
                }
                else
                {
                    break;
                }
            }
        }

        return fields;
    }

    private static string? NullIfEmpty(string value)
    {
        string trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
