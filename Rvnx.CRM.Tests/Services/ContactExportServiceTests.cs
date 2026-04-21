using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;

namespace Rvnx.CRM.Tests.Services;

public class ContactExportServiceTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly Mock<IVCardService> _vCardServiceMock = new();
    private readonly ContactExportService _service;

    public ContactExportServiceTests()
    {
        _service = new ContactExportService(_repositoryMock.Object, _vCardServiceMock.Object);
    }

    private void SetupBulkRepository(
        List<Contact> contacts,
        List<ContactMethod>? methods = null,
        List<SignificantDate>? significantDates = null,
        List<Attachment>? attachments = null)
    {
        _repositoryMock
            .Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken _, string[] _) =>
                contacts.Where(predicate.Compile()).ToList());

        SetupChunkedList(methods);
        SetupChunkedList(significantDates);
        SetupChunkedList(attachments);
    }

    private void SetupChunkedList<T>(List<T>? source) where T : BaseEntity
    {
        _repositoryMock
            .Setup(r => r.ListAsNoTrackingAsync<T>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken _, string[] _) =>
                (source ?? []).Where(predicate.Compile()).ToList());
    }

    private static List<ZipArchiveEntry> ReadZipEntries(byte[] zipBytes)
    {
        using MemoryStream ms = new(zipBytes);
        using ZipArchive archive = new(ms, ZipArchiveMode.Read);
        return [.. archive.Entries];
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncIncludesHiddenAndExcludesPartial()
    {
        Contact normal = new() { Id = Guid.NewGuid(), FirstName = "Normal", LastName = "One" };
        Contact hidden = new() { Id = Guid.NewGuid(), FirstName = "Hidden", LastName = "Two", IsHidden = true };
        Contact partial = new() { Id = Guid.NewGuid(), FirstName = "Partial", LastName = "Three", IsPartial = true };

        SetupBulkRepository([normal, hidden, partial]);
        _vCardServiceMock.Setup(v => v.ExportVCard(It.IsAny<Contact>()))
            .Returns<Contact>(c => Encoding.UTF8.GetBytes($"VCARD:{c.FirstName}"));

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        List<ZipArchiveEntry> entries = ReadZipEntries(result.FileContent);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Name == "Normal_One.vcf");
        Assert.Contains(entries, e => e.Name == "Hidden_Two.vcf");
        Assert.DoesNotContain(entries, e => e.Name.StartsWith("Partial", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncDisambiguatesDuplicateNames()
    {
        Contact a = new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" };
        Contact b = new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" };
        Contact c = new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Smith" };

        SetupBulkRepository([a, b, c]);
        _vCardServiceMock.Setup(v => v.ExportVCard(It.IsAny<Contact>())).Returns([1, 2, 3]);

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        List<string> names = [.. ReadZipEntries(result.FileContent).Select(e => e.Name)];
        Assert.Equal(["John_Smith.vcf", "John_Smith_2.vcf", "John_Smith_3.vcf"], names);
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncSanitizesInvalidFileNameChars()
    {
        Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Jo/hn", LastName = "Sm:ith" };

        SetupBulkRepository([contact]);
        _vCardServiceMock.Setup(v => v.ExportVCard(It.IsAny<Contact>())).Returns([0]);

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        ZipArchiveEntry entry = Assert.Single(ReadZipEntries(result.FileContent));
        char[] invalid = Path.GetInvalidFileNameChars();
        Assert.DoesNotContain(entry.Name, c => invalid.Contains(c));
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncFallsBackToContactIdWhenNamesBlank()
    {
        Contact contact = new() { Id = Guid.NewGuid(), FirstName = string.Empty, LastName = null };

        SetupBulkRepository([contact]);
        _vCardServiceMock.Setup(v => v.ExportVCard(It.IsAny<Contact>())).Returns([0]);

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        ZipArchiveEntry entry = Assert.Single(ReadZipEntries(result.FileContent));
        Assert.StartsWith("contact-", entry.Name);
        Assert.EndsWith(".vcf", entry.Name);
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncEmptyContactListProducesValidEmptyZip()
    {
        SetupBulkRepository([]);

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        Assert.Empty(ReadZipEntries(result.FileContent));
        Assert.Equal("application/zip", result.ContentType);
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncContentTypeAndFileNameAreCorrect()
    {
        SetupBulkRepository([]);

        ContactExportResult result = await _service.ExportAllToVCardZipAsync();

        Assert.Equal("application/zip", result.ContentType);
        Assert.StartsWith("contacts_vcard_", result.FileName);
        Assert.EndsWith(".zip", result.FileName);
    }

    [Fact]
    public async Task ExportAllToVCardZipAsyncPopulatesContactRelationshipsBeforeSerializing()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "Person" };
        ContactMethod method = new() { ContactId = contactId, Type = Core.Enumerations.ContactMethodType.Email, Value = "t@example.com" };
        SignificantDate date = new() { ContactId = contactId, Title = SignificantDateTitles.Birthday, EventDate = new DateOnly(2000, 1, 1) };
        Attachment attachment = new() { ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };

        SetupBulkRepository([contact], methods: [method], significantDates: [date], attachments: [attachment]);

        Contact? captured = null;
        _vCardServiceMock.Setup(v => v.ExportVCard(It.IsAny<Contact>()))
            .Callback<Contact>(c => captured = c)
            .Returns([0]);

        await _service.ExportAllToVCardZipAsync();

        Assert.NotNull(captured);
        Assert.Single(captured!.ContactMethods);
        Assert.Single(captured.SignificantDates);
        Assert.Single(captured.Attachments);
    }
}
