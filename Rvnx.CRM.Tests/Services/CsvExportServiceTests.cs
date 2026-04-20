using System.Text;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class CsvExportServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly CsvExportService _service;

    private static readonly string[] ExpectedHeaders =
    [
        "contact_id",
        "first_name",
        "last_name",
        "nickname",
        "company",
        "job_title",
        "pronouns",
        "gender",
        "religion",
        "birthday",
        "is_hidden",
        "is_partial",
        "email_1",
        "email_2",
        "email_3",
        "email_4",
        "email_5",
        "phone_1",
        "phone_2",
        "phone_3",
        "phone_4",
        "phone_5",
        "address_1_line1",
        "address_1_line2",
        "address_1_city",
        "address_1_state",
        "address_1_zip",
        "address_1_country",
        "address_1_type"
    ];

    public CsvExportServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
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
                It.IsAny<Expression<Func<ContactMethod, ContactMethod>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ContactMethod, bool>> predicate,
                Expression<Func<ContactMethod, ContactMethod>> selector,
                CancellationToken _) =>
            {
                Func<ContactMethod, bool> compiled = predicate.Compile();
                Func<ContactMethod, ContactMethod> sel = selector.Compile();
                return (methods ?? []).Where(compiled).Select(sel).ToList();
            });

        _repositoryMock
            .Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Address, bool>>>(),
                It.IsAny<Expression<Func<Address, Address>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Address, bool>> predicate,
                Expression<Func<Address, Address>> selector,
                CancellationToken _) =>
            {
                Func<Address, bool> compiled = predicate.Compile();
                Func<Address, Address> sel = selector.Compile();
                return (addresses ?? []).Where(compiled).Select(sel).ToList();
            });

        _repositoryMock
            .Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate,
                Expression<Func<SignificantDate, (Guid, DateOnly)>> selector,
                CancellationToken _) =>
            {
                Func<SignificantDate, bool> compiled = predicate.Compile();
                Func<SignificantDate, (Guid, DateOnly)> sel = selector.Compile();
                return (significantDates ?? []).Where(compiled).Select(sel).ToList();
            });
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

    [Fact]
    public async Task ExportContactsAsyncProducesExpectedHeaderRow()
    {
        SetupRepository([]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        string header = lines[0];

        Assert.Equal(string.Join(",", ExpectedHeaders), header);
    }

    [Fact]
    public async Task ExportContactsAsyncQuotesFieldsWithCommas()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Test",
            LastName = "User",
            Company = "Acme, Inc."
        };

        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        Assert.Contains("\"Acme, Inc.\"", csv);
    }

    [Fact]
    public async Task ExportContactsAsyncEscapesDoubleQuotes()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Test",
            LastName = "User",
            Company = "He said \"hi\""
        };

        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        Assert.Contains("\"He said \"\"hi\"\"\"", csv);
    }

    [Fact]
    public async Task ExportContactsAsyncMultipleEmailsLandInSeparateColumns()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Multi",
            LastName = "Email"
        };

        DateTime baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        List<ContactMethod> methods =
        [
            new() { Id = Guid.NewGuid(), ContactId = contactId, Type = ContactMethodType.Email, Value = "a@example.com", CreatedDate = baseTime },
            new() { Id = Guid.NewGuid(), ContactId = contactId, Type = ContactMethodType.Email, Value = "b@example.com", CreatedDate = baseTime.AddSeconds(1) },
            new() { Id = Guid.NewGuid(), ContactId = contactId, Type = ContactMethodType.Email, Value = "c@example.com", CreatedDate = baseTime.AddSeconds(2) }
        ];

        SetupRepository([contact], methods: methods);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        string[] headers = lines[0].Split(',');
        string[] values = lines[1].Split(',');

        int email1 = Array.IndexOf(headers, "email_1");
        int email2 = Array.IndexOf(headers, "email_2");
        int email3 = Array.IndexOf(headers, "email_3");
        int email4 = Array.IndexOf(headers, "email_4");
        int email5 = Array.IndexOf(headers, "email_5");

        Assert.Equal("a@example.com", values[email1]);
        Assert.Equal("b@example.com", values[email2]);
        Assert.Equal("c@example.com", values[email3]);
        Assert.Equal(string.Empty, values[email4]);
        Assert.Equal(string.Empty, values[email5]);
    }

    [Fact]
    public async Task ExportContactsAsyncContactIdColumnContainsGuidAsString()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Id",
            LastName = "Test"
        };

        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        string[] headers = lines[0].Split(',');
        string[] values = lines[1].Split(',');

        int idIndex = Array.IndexOf(headers, "contact_id");
        Assert.Equal(contactId.ToString(), values[idIndex]);
    }

    [Fact]
    public async Task ExportContactsAsyncBooleansAreLowercase()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Bool",
            LastName = "Test",
            IsHidden = false,
            IsPartial = false
        };

        SetupRepository([contact]);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        string[] headers = lines[0].Split(',');
        string[] values = lines[1].Split(',');

        int hiddenIndex = Array.IndexOf(headers, "is_hidden");
        int partialIndex = Array.IndexOf(headers, "is_partial");

        Assert.Equal("false", values[hiddenIndex]);
        Assert.Equal("false", values[partialIndex]);
    }

    [Fact]
    public async Task ExportContactsAsyncIncludesBirthdayFormattedIso()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Birthday",
            LastName = "Test"
        };

        List<SignificantDate> dates =
        [
            new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = SignificantDateTitles.Birthday,
                EventDate = new DateOnly(1990, 6, 15)
            }
        ];

        SetupRepository([contact], significantDates: dates);

        ContactExportResult result = await _service.ExportContactsAsync();

        string csv = BytesToCsvString(result.FileContent);
        string[] lines = csv.Split("\r\n", StringSplitOptions.None);
        string[] headers = lines[0].Split(',');
        string[] values = lines[1].Split(',');

        int bdayIndex = Array.IndexOf(headers, "birthday");
        Assert.Equal("1990-06-15", values[bdayIndex]);
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

    [Fact]
    public void ColumnsAreExposedForReuse()
    {
        Assert.NotEmpty(CsvExportService.Columns);
        Assert.Equal(ExpectedHeaders.Length, CsvExportService.Columns.Count);
        for (int i = 0; i < ExpectedHeaders.Length; i++)
        {
            Assert.Equal(ExpectedHeaders[i], CsvExportService.Columns[i].Header);
        }
    }
}
