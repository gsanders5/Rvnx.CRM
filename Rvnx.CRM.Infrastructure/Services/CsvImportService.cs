using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
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

    // Column index constants matching CsvExportService.ColumnHeaders order
    private const int ColContactId = 0;
    private const int ColFirstName = 1;
    private const int ColLastName = 2;
    private const int ColNickname = 3;
    private const int ColCompany = 4;
    private const int ColJobTitle = 5;
    private const int ColPronouns = 6;
    private const int ColGender = 7;
    private const int ColReligion = 8;
    private const int ColBirthday = 9;
    private const int ColIsHidden = 10;
    // ColIsPartial = 11 — skipped intentionally
    private const int ColEmail1 = 12;
    private const int ColEmail2 = 13;
    private const int ColEmail3 = 14;
    private const int ColEmail4 = 15;
    private const int ColEmail5 = 16;
    private const int ColPhone1 = 17;
    private const int ColPhone2 = 18;
    private const int ColPhone3 = 19;
    private const int ColPhone4 = 20;
    private const int ColPhone5 = 21;
    private const int ColAddrLine1 = 22;
    private const int ColAddrLine2 = 23;
    private const int ColAddrCity = 24;
    private const int ColAddrState = 25;
    private const int ColAddrZip = 26;
    private const int ColAddrCountry = 27;
    private const int ColAddrType = 28;
    private const int ExpectedColumnCount = 29;

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

                // Collect contact methods for duplicate detection
                List<ContactMethod> methods = BuildContactMethods(fields);
                contact.ContactMethods = methods;

                if (await IsDuplicateAsync(contact))
                {
                    skippedCount++;
                    continue;
                }

                // Clear the NotMapped collection before saving to avoid EF confusion
                contact.ContactMethods = [];

                await _repository.AddAsync(contact);
                await _repository.SaveChangesAsync();

                addedCount++;

                // Save related entities now that we have the contact ID
                await SaveContactMethodsAsync(contact.Id, methods);
                await SaveBirthdayAsync(contact.Id, fields[ColBirthday]);
                await SaveAddressAsync(contact.Id, fields);

                await _repository.SaveChangesAsync();
            }

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

    private static List<ContactMethod> BuildContactMethods(List<string> fields)
    {
        List<ContactMethod> methods = [];

        int[] emailCols = [ColEmail1, ColEmail2, ColEmail3, ColEmail4, ColEmail5];
        foreach (int col in emailCols)
        {
            string val = fields[col].Trim();
            if (!string.IsNullOrEmpty(val))
            {
                methods.Add(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    Type = ContactMethodType.Email,
                    Value = val,
                    Label = ContactMethodLabels.Primary
                });
            }
        }

        int[] phoneCols = [ColPhone1, ColPhone2, ColPhone3, ColPhone4, ColPhone5];
        foreach (int col in phoneCols)
        {
            string val = fields[col].Trim();
            if (!string.IsNullOrEmpty(val))
            {
                methods.Add(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    Type = ContactMethodType.Phone,
                    Value = val,
                    Label = ContactMethodLabels.Primary
                });
            }
        }

        return methods;
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

    private async Task<bool> IsDuplicateAsync(Contact candidate)
    {
        if (await _repository.CountAsync<Contact>(c => c.FirstName == candidate.FirstName && c.LastName == candidate.LastName) > 0)
        {
            return true;
        }

        if (candidate.ContactMethods != null && candidate.ContactMethods.Count > 0)
        {
            List<string> valuesToCheck = candidate.ContactMethods.Select(m => m.Value).ToList();
            return await _repository.CountAsync<ContactMethod>(cm =>
                valuesToCheck.Contains(cm.Value)) > 0;
        }

        return false;
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
