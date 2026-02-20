using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Services;

public class ContactImportServiceTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly Mock<IVCardService> _vCardMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly ContactImportService _service;

    public ContactImportServiceTests()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));

        _context = new CRMDbContext(options, _userMock.Object);
        Repository repository = new Repository(_context);

        _service = new ContactImportService(repository, _vCardMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ImportContactsAsync_WhenContactExists_ShouldSkipDuplicates()
    {
        // Arrange
        Guid existingId = Guid.NewGuid();
        _context.Contacts.Add(new Contact { Id = existingId, FirstName = "John", LastName = "Doe" });
        await _context.SaveChangesAsync();

        // Incoming contacts (one duplicate, one new)
        List<Contact> importedContacts = new()
        {
            new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" }, // Duplicate
            new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" }  // New
        };

        _vCardMock.Setup(s => s.ParseVCard(It.IsAny<Stream>())).Returns(importedContacts);

        // Act
        (int Added, int Skipped) result = await _service.ImportContactsAsync(new MemoryStream());

        // Assert
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);

        List<Contact> allContacts = await _context.Contacts.ToListAsync();
        Assert.Equal(2, allContacts.Count);
        Assert.Contains(allContacts, c => c.FirstName == "John" && c.LastName == "Doe");
        Assert.Contains(allContacts, c => c.FirstName == "Jane" && c.LastName == "Doe");
    }

    [Fact]
    public async Task ImportContactsAsync_ShouldPersistRelatedEntities()
    {
        // Arrange
        Contact contact = new Contact { Id = Guid.NewGuid(), FirstName = "With", LastName = "Related" };
        contact.ContactMethods = new List<ContactMethod>
        {
            new ContactMethod { Type = ContactMethodType.Email, Value = "test@test.com" }
        };

        List<Contact> importedContacts = new() { contact };
        _vCardMock.Setup(s => s.ParseVCard(It.IsAny<Stream>())).Returns(importedContacts);

        // Act
        await _service.ImportContactsAsync(new MemoryStream());

        // Assert
        Contact? savedContact = await _context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "With");
        Assert.NotNull(savedContact);

        List<ContactMethod> methods = await _context.Set<ContactMethod>().Where(m => m.EntityId == savedContact.Id).ToListAsync();
        Assert.Single(methods);
        Assert.Equal("test@test.com", methods[0].Value);
    }
}
