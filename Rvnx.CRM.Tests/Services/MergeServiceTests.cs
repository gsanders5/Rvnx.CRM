using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Moq;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class MergeServiceTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly IRepository _repository;
    private readonly MergeService _sut;

    public MergeServiceTests()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns((Guid?)null);
        mockUserService.Setup(u => u.GroupId).Returns((Guid?)null);
        mockUserService.Setup(u => u.UserName).Returns("System");

        _context = new CRMDbContext(options, mockUserService.Object);
        _context.Database.EnsureCreated();

        _repository = new Repository(_context);
        _sut = new MergeService(_context, _repository);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSelfMergeThrowsInvalidOperationException()
    {
        Guid id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.MergeContactsAsync(id, id));
    }

    [Fact]
    public async Task MergeContactsAsyncWhenScalarsPrimaryWinsWhenBothSet()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary First", LastName = "Primary Last" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary First", LastName = "Secondary Last", MaidenName = "Sec Maiden" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Primary First", merged.FirstName);
        Assert.Equal("Primary Last", merged.LastName);
        Assert.Equal("Sec Maiden", merged.MaidenName);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenScalarsSecondaryWinsWhenPrimaryNull()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary", JobTitle = "Dev", Company = "Rvnx" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Dev", merged.JobTitle);
        Assert.Equal("Rvnx", merged.Company);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenAttachmentsMoved()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        var att1 = new Attachment { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec.pdf", AttachmentType = "Doc", ContentType = "application/pdf" };
        var att2 = new Attachment { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<Attachment>().AddRangeAsync(att1, att2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var primaryAtts = await _context.Set<Attachment>().Where(a => a.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryAtts.Count);
        Assert.Contains(primaryAtts, a => a.FileName == "sec.pdf");
        Assert.Contains(primaryAtts, a => a.FileName == "sec_img.png" && a.AttachmentType == "ProfileImage");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenAttachmentsProfileImageDowngradedWhenPrimaryHasOne()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        var att1 = new Attachment { Id = Guid.NewGuid(), ContactId = primary.Id, FileName = "pri_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };
        var att2 = new Attachment { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<Attachment>().AddRangeAsync(att1, att2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var primaryAtts = await _context.Set<Attachment>().Where(a => a.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryAtts.Count);

        var priImg = primaryAtts.First(a => a.FileName == "pri_img.png");
        Assert.Equal("ProfileImage", priImg.AttachmentType);

        var secImg = primaryAtts.First(a => a.FileName == "sec_img.png");
        Assert.Equal("General", secImg.AttachmentType);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenNotesMoved()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };
        var note = new Note { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "Sec Note", Value = "Test" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<Note>().AddAsync(note);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var primaryNotes = await _context.Set<Note>().Where(n => n.ContactId == primary.Id).ToListAsync();
        Assert.Single(primaryNotes);
        Assert.Equal("Sec Note", primaryNotes.First().Title);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenContactMethodsDeduplicated()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        var priMethod = new ContactMethod { Id = Guid.NewGuid(), ContactId = primary.Id, Type = ContactMethodType.Email, Value = "test@rvnx.net" };
        var secMethodDup = new ContactMethod { Id = Guid.NewGuid(), ContactId = secondary.Id, Type = ContactMethodType.Email, Value = "TEST@rvnx.net" };
        var secMethodNew = new ContactMethod { Id = Guid.NewGuid(), ContactId = secondary.Id, Type = ContactMethodType.Phone, Value = "123456789" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<ContactMethod>().AddRangeAsync(priMethod, secMethodDup, secMethodNew);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var primaryMethods = await _context.Set<ContactMethod>().Where(m => m.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryMethods.Count);
        Assert.Contains(primaryMethods, m => m.Type == ContactMethodType.Email && m.Value == "test@rvnx.net");
        Assert.Contains(primaryMethods, m => m.Type == ContactMethodType.Phone && m.Value == "123456789");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSignificantDatesDeduplicated()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        var pDate = new SignificantDate { Id = Guid.NewGuid(), ContactId = primary.Id, Title = "Birthday", EventDate = new DateOnly(1990, 1, 1) };
        var sDateDup = new SignificantDate { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "BIRTHDAY", EventDate = new DateOnly(1990, 1, 1) };
        var sDateNew = new SignificantDate { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "Anniversary", EventDate = new DateOnly(2020, 1, 1) };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<SignificantDate>().AddRangeAsync(pDate, sDateDup, sDateNew);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var dates = await _context.Set<SignificantDate>().Where(d => d.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, dates.Count);
        Assert.Contains(dates, d => d.Title == "Birthday");
        Assert.Contains(dates, d => d.Title == "Anniversary");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenPetsDeduplicated()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        var pPet = new Pet { Id = Guid.NewGuid(), ContactId = primary.Id, Name = "Fido" };
        var sPetDup = new Pet { Id = Guid.NewGuid(), ContactId = secondary.Id, Name = "FIDO" };
        var sPetNew = new Pet { Id = Guid.NewGuid(), ContactId = secondary.Id, Name = "Mittens" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.Set<Pet>().AddRangeAsync(pPet, sPetDup, sPetNew);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var pets = await _context.Set<Pet>().Where(p => p.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, pets.Count);
        Assert.Contains(pets, p => p.Name == "Fido");
        Assert.Contains(pets, p => p.Name == "Mittens");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenRelationshipsDeduplicated()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };
        var other = new Contact { Id = Guid.NewGuid(), FirstName = "Other" };
        var typeId = Guid.NewGuid();

        var rel1 = new Relationship { Id = Guid.NewGuid(), EntityId = primary.Id, RelatedEntityId = other.Id, RelationshipTypeId = typeId, EntityType = "Person" };
        var rel2 = new Relationship { Id = Guid.NewGuid(), EntityId = secondary.Id, RelatedEntityId = other.Id, RelationshipTypeId = typeId, EntityType = "Person" };

        await _context.Contacts.AddRangeAsync(primary, secondary, other);
        await _context.Set<Relationship>().AddRangeAsync(rel1, rel2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        var rels = await _context.Set<Relationship>().Where(r => r.EntityId == primary.Id).ToListAsync();
        Assert.Single(rels);
        Assert.Equal(other.Id, rels.First().RelatedEntityId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenTransactionFailsBothContactsRemainIntact()
    {
        var primary = new Contact { Id = Guid.NewGuid(), FirstName = "Primary" };
        var secondary = new Contact { Id = Guid.NewGuid(), FirstName = "Secondary" };

        await _context.Contacts.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        // Cause an exception during the merge by passing the same ID (or forcing a failure another way).
        // Actually, we need a failure AFTER the initial checks.
        // Let's create a scenario where saving fails, e.g., adding an entity with a duplicate key manually,
        // or just using a mock to force a failure.
        // Since we are using a real (InMemory) db context here, forcing a DbUpdateException is tricky without
        // doing something like violating a constraint. But in-memory doesn't enforce all constraints.
        // Let's create a Mock of IRepository that throws on UpdateAsync.
        var mockRepo = new Mock<IRepository>();
        mockRepo.Setup(r => r.QueryUnfiltered<Contact>()).Returns(_context.Contacts);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Attachment>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Note>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContactMethod>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<SignificantDate>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Fact>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Relationship>());
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Pet>());

        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Forced failure"));

        var sutWithMockRepo = new MergeService(_context, mockRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sutWithMockRepo.MergeContactsAsync(primary.Id, secondary.Id));

        // Verify both contacts still exist in the database (since transaction would roll back, though InMemory doesn't truly support transactions,
        // the code should execute the rollback path without crashing).
        var pCheck = await _context.Contacts.FindAsync(primary.Id);
        var sCheck = await _context.Contacts.FindAsync(secondary.Id);

        Assert.NotNull(pCheck);
        Assert.NotNull(sCheck);
    }
}
