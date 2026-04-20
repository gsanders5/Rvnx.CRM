using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;
using System.Text;

namespace Rvnx.CRM.Tests.Services;

public class CsvExportServiceTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly CsvExportService _service;

    public CsvExportServiceTests()
    {
        _service = new CsvExportService(_repositoryMock.Object);
    }

    private void SetupRepository(
        List<Contact> contacts,
        List<ContactMethod>? methods = null,
        List<Address>? addresses = null,
        List<SignificantDate>? significantDates = null)
    {
        _repositoryMock
            .Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(contacts);

        _repositoryMock
            .Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<ContactMethod, bool>>>(),
                It.IsAny<Expression<Func<ContactMethod, (Guid, ContactMethodType, string, DateTime)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<ContactMethod, bool>> predicate,
                Expression<Func<ContactMethod, (Guid, ContactMethodType, string, DateTime)>> selector,
                CancellationToken _) => (methods ?? [])
                    .Where(predicate.Compile())
                    .Select(selector.Compile())
                    .ToList());

        _repositoryMock
            .Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Address, bool>>>(),
                It.IsAny<Expression<Func<Address, Address>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<Address, bool>> predicate,
                Expression<Func<Address, Address>> selector,
                CancellationToken _) => (addresses ?? [])
                    .Where(predicate.Compile())
                    .Select(selector.Compile())
                    .ToList());

        _repositoryMock
            .Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Expression<Func<SignificantDate, bool>> predicate,
                Expression<Func<SignificantDate, (Guid, DateOnly)>> selector,
                CancellationToken _) => (significantDates ?? [])
                    .Where(predicate.Compile())
                    .Select(selector.Compile())
                    .ToList());
    }

    private static string BytesToCsvString(byte[] bytes)
    {
        byte[] bom = Encoding.UTF8.GetPreamble();
        Assert.True(bytes.Length >= bom.Length);
        for (int i = 0; i < bom.Length; i++)
        {
            Assert.Equal(bom[i], bytes[i]);
        }
        return Encoding.UTF8.GetString(bytes, bom.Length, bytes.Length - bom.Length);
    }

    private static string GetField(string csv, int rowIndex, string header)
    {
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        int columnIndex = Array.IndexOf(lines[0].Split(','), header);
        Assert.True(columnIndex >= 0, $"Header '{header}' not found");
        return lines[rowIndex].Split(',')[columnIndex];
    }

    [Fact]
    public async Task ExportContactsAsyncHeaderRowMatchesPublicColumnHeaders()
    {
        SetupRepository([]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string header = csv.Split("\r\n", StringSplitOptions.None)[0];
        Assert.Equal(string.Join(",", CsvExportService.ColumnHeaders), header);
    }

    [Fact]
    public async Task ExportContactsAsyncQuotesFieldsWithCommas()
    {
        Contact contact = new() { Id = Guid.NewGuid(), Company = "Acme, Inc." };
        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        Assert.Contains("\"Acme, Inc.\"", BytesToCsvString(result.FileContent));
    }

    [Fact]
    public async Task ExportContactsAsyncEscapesDoubleQuotes()
    {
        Contact contact = new() { Id = Guid.NewGuid(), Company = "He said \"hi\"" };
        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        Assert.Contains("\"He said \"\"hi\"\"\"", BytesToCsvString(result.FileContent));
    }

    [Fact]
    public async Task ExportContactsAsyncMultipleEmailsLandInSeparateColumns()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new() { Id = contactId };
        DateTime baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        List<ContactMethod> methods =
        [
            new() { ContactId = contactId, Type = ContactMethodType.Email, Value = "a@example.com", CreatedDate = baseTime },
            new() { ContactId = contactId, Type = ContactMethodType.Email, Value = "b@example.com", CreatedDate = baseTime.AddSeconds(1) },
            new() { ContactId = contactId, Type = ContactMethodType.Email, Value = "c@example.com", CreatedDate = baseTime.AddSeconds(2) },
        ];

        SetupRepository([contact], methods: methods);

        ContactExportResult result = await _service.ExportContactsAsync();
        string csv = BytesToCsvString(result.FileContent);

        Assert.Equal("a@example.com", GetField(csv, 1, "email_1"));
        Assert.Equal("b@example.com", GetField(csv, 1, "email_2"));
        Assert.Equal("c@example.com", GetField(csv, 1, "email_3"));
        Assert.Equal(string.Empty, GetField(csv, 1, "email_4"));
        Assert.Equal(string.Empty, GetField(csv, 1, "email_5"));
    }

    [Fact]
    public async Task ExportContactsAsyncContactIdColumnContainsGuidAsString()
    {
        Guid contactId = Guid.NewGuid();
        SetupRepository([new Contact { Id = contactId }]);

        ContactExportResult result = await _service.ExportContactsAsync();

        Assert.Equal(contactId.ToString(), GetField(BytesToCsvString(result.FileContent), 1, "contact_id"));
    }

    [Fact]
    public async Task ExportContactsAsyncBooleansAreLowercase()
    {
        SetupRepository([new Contact { Id = Guid.NewGuid(), IsHidden = false, IsPartial = false }]);

        ContactExportResult result = await _service.ExportContactsAsync();
        string csv = BytesToCsvString(result.FileContent);

        Assert.Equal("false", GetField(csv, 1, "is_hidden"));
        Assert.Equal("false", GetField(csv, 1, "is_partial"));
    }

    [Fact]
    public async Task ExportContactsAsyncIncludesBirthdayFormattedIso()
    {
        Guid contactId = Guid.NewGuid();
        List<SignificantDate> dates =
        [
            new()
            {
                ContactId = contactId,
                Title = SignificantDateTitles.Birthday,
                EventDate = new DateOnly(1990, 6, 15),
            },
        ];

        SetupRepository([new Contact { Id = contactId }], significantDates: dates);

        ContactExportResult result = await _service.ExportContactsAsync();

        Assert.Equal("1990-06-15", GetField(BytesToCsvString(result.FileContent), 1, "birthday"));
    }

    [Fact]
    public async Task ExportContactsAsyncContentTypeAndFileNameAreCorrect()
    {
        SetupRepository([]);

        ContactExportResult result = await _service.ExportContactsAsync();

        Assert.Equal("text/csv", result.ContentType);
        Assert.StartsWith("contacts_", result.FileName);
        Assert.EndsWith(".csv", result.FileName);
    }
}