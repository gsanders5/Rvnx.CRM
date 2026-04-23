using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactImportServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<IVCardService> _vCardServiceMock;
    private readonly Mock<ILogger<ContactImportService>> _loggerMock;
    private readonly ContactImportService _service;

    public ContactImportServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _vCardServiceMock = new Mock<IVCardService>();
        _loggerMock = new Mock<ILogger<ContactImportService>>();

        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _service = new ContactImportService(_repositoryMock.Object, _vCardServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ImportFromVCardAsyncValidContactsAddsToRepository()
    {
        List<Contact> contacts =
        [
            new() { FirstName = "John", LastName = "Doe" },
            new() { FirstName = "Jane", LastName = "Smith" }
        ];

        _vCardServiceMock.Setup(v => v.ParseVCardAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
            .ReturnsAsync(0);
        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
            .ReturnsAsync(0);

        using MemoryStream stream = new();

        ContactImportResult result = await _service.ImportFromVCardAsync(stream);

        Assert.Equal(2, result.AddedCount);
        Assert.Equal(0, result.SkippedCount);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "John"), default), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Jane"), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task ImportFromVCardAsyncDuplicateNameSkipsImport()
    {
        List<Contact> contacts =
        [
            new() { FirstName = "Duplicate", LastName = "User" }
        ];

        _vCardServiceMock.Setup(v => v.ParseVCardAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
            .ReturnsAsync(1);

        using MemoryStream stream = new();

        ContactImportResult result = await _service.ImportFromVCardAsync(stream);

        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), default), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ImportFromVCardAsyncDuplicateContactMethodSkipsImport()
    {
        Contact contact = new()
        {
            FirstName = "Unique",
            LastName = "User",
            ContactMethods =
            [
                new() { Type = ContactMethodType.Email, Value = "test@example.com" }
            ]
        };
        List<Contact> contacts = [contact];

        _vCardServiceMock.Setup(v => v.ParseVCardAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
            .ReturnsAsync(0);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
            .ReturnsAsync(1);

        using MemoryStream stream = new();

        ContactImportResult result = await _service.ImportFromVCardAsync(stream);

        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), default), Times.Never);
    }

    [Fact]
    public async Task ImportFromVCardAsyncHandlesMultipleDuplicatesSameVCardFile()
    {
        Contact contact = new()
        {
            FirstName = "Alice",
            LastName = "Jones",
            ContactMethods =
            [
                new() { Type = ContactMethodType.Email, Value = "alice@example.com" }
            ]
        };
        List<Contact> contacts = [contact, contact];

        _vCardServiceMock.Setup(v => v.ParseVCardAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        _repositoryMock.SetupSequence(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
            .ReturnsAsync(0)   // first contact: no name match
            .ReturnsAsync(1);  // second contact: name already exists (added by first save)

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
            .ReturnsAsync(0);

        using MemoryStream stream = new();

        ContactImportResult result = await _service.ImportFromVCardAsync(stream);

        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ImportFromVCardAsyncExceptionInParsingLogsAndRethrows()
    {
        _vCardServiceMock.Setup(v => v.ParseVCardAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Parse error"));

        using MemoryStream stream = new();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ImportFromVCardAsync(stream));
        Assert.Equal("Parse error", ex.Message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error importing VCF")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
