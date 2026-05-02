using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;

namespace Rvnx.CRM.Tests.Services;

public class MergeServiceTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly IRepository _repository;
    private readonly MergeService _sut;

    public MergeServiceTests()
    {
        _context = TestDbContextFactory.CreateForSystemUser(ensureCreated: true);
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
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary First", LastName = "Primary Last" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary First", LastName = "Secondary Last", MaidenName = "Sec Maiden" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Primary First", merged.FirstName);
        Assert.Equal("Primary Last", merged.LastName);
        Assert.Equal("Sec Maiden", merged.MaidenName);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenScalarsSecondaryWinsWhenPrimaryNull()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary", JobTitle = "Dev", Company = "Rvnx" };

        await _context!.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Dev", merged.JobTitle);
        Assert.Equal("Rvnx", merged.Company);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenAttachmentsMoved()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        Attachment att1 = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec.pdf", AttachmentType = "Doc", ContentType = "application/pdf" };
        Attachment att2 = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<Attachment>().AddRangeAsync(att1, att2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<Attachment> primaryAtts = await _context.Set<Attachment>().Where(a => a.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryAtts.Count);
        Assert.Contains(primaryAtts, a => a.FileName == "sec.pdf");
        Assert.Contains(primaryAtts, a => a.FileName == "sec_img.png" && a.AttachmentType == "ProfileImage");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenAttachmentsProfileImageDowngradedWhenPrimaryHasOne()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        Attachment att1 = new()
        { Id = Guid.NewGuid(), ContactId = primary.Id, FileName = "pri_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };
        Attachment att2 = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, FileName = "sec_img.png", AttachmentType = "ProfileImage", ContentType = "image/png" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<Attachment>().AddRangeAsync(att1, att2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<Attachment> primaryAtts = await _context.Set<Attachment>().Where(a => a.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryAtts.Count);

        Attachment priImg = primaryAtts.First(a => a.FileName == "pri_img.png");
        Assert.Equal("ProfileImage", priImg.AttachmentType);

        Attachment secImg = primaryAtts.First(a => a.FileName == "sec_img.png");
        Assert.Equal("General", secImg.AttachmentType);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenNotesMoved()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };
        Note note = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "Sec Note", Value = "Test" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<Note>().AddAsync(note);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<Note> primaryNotes = await _context.Set<Note>().Where(n => n.ContactId == primary.Id).ToListAsync();
        Assert.Single(primaryNotes);
        Assert.Equal("Sec Note", primaryNotes.First().Title);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenContactMethodsDeduplicated()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        ContactMethod priMethod = new()
        { Id = Guid.NewGuid(), ContactId = primary.Id, Type = ContactMethodType.Email, Value = "test@rvnx.net" };
        ContactMethod secMethodDup = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, Type = ContactMethodType.Email, Value = "TEST@rvnx.net" };
        ContactMethod secMethodNew = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, Type = ContactMethodType.Phone, Value = "123456789" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<ContactMethod>().AddRangeAsync(priMethod, secMethodDup, secMethodNew);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<ContactMethod> primaryMethods = await _context.Set<ContactMethod>().Where(m => m.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, primaryMethods.Count);
        Assert.Contains(primaryMethods, m => m.Type == ContactMethodType.Email && m.Value == "test@rvnx.net");
        Assert.Contains(primaryMethods, m => m.Type == ContactMethodType.Phone && m.Value == "123456789");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSignificantDatesDeduplicated()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        SignificantDate pDate = new()
        { Id = Guid.NewGuid(), ContactId = primary.Id, Title = "Birthday", EventDate = new DateOnly(1990, 1, 1) };
        SignificantDate sDateDup = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "BIRTHDAY", EventDate = new DateOnly(1990, 1, 1) };
        SignificantDate sDateNew = new()
        { Id = Guid.NewGuid(), ContactId = secondary.Id, Title = "Anniversary", EventDate = new DateOnly(2020, 1, 1) };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<SignificantDate>().AddRangeAsync(pDate, sDateDup, sDateNew);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<SignificantDate> dates = await _context.Set<SignificantDate>().Where(d => d.ContactId == primary.Id).ToListAsync();
        Assert.Equal(2, dates.Count);
        Assert.Contains(dates, d => d.Title == "Birthday");
        Assert.Contains(dates, d => d.Title == "Anniversary");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenPetsDeduplicated()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        Pet pPet = new()
        { Id = Guid.NewGuid(), Name = "Fido" };
        Pet sPetDup = new()
        { Id = Guid.NewGuid(), Name = "FIDO" };
        Pet sPetNew = new()
        { Id = Guid.NewGuid(), Name = "Mittens" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<Pet>().AddRangeAsync(pPet, sPetDup, sPetNew);
        await _context.Set<PetContact>().AddRangeAsync(
            new PetContact { PetId = pPet.Id, ContactId = primary.Id },
            new PetContact { PetId = sPetDup.Id, ContactId = secondary.Id },
            new PetContact { PetId = sPetNew.Id, ContactId = secondary.Id });
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<PetContact> petContacts = await _context.Set<PetContact>()
            .Where(pc => pc.ContactId == primary.Id)
            .Include(pc => pc.Pet)
            .ToListAsync();
        Assert.Equal(2, petContacts.Count);
        Assert.Contains(petContacts, pc => pc.Pet.Name == "Fido");
        Assert.Contains(petContacts, pc => pc.Pet.Name == "Mittens");
    }

    [Fact]
    public async Task MergeContactsAsyncWhenRelationshipsDeduplicated()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };
        Contact other = new()
        { Id = Guid.NewGuid(), FirstName = "Other" };
        Guid typeId = Guid.NewGuid();

        Relationship rel1 = new()
        { Id = Guid.NewGuid(), EntityId = primary.Id, RelatedEntityId = other.Id, RelationshipTypeId = typeId, EntityType = EntityType.Person };
        Relationship rel2 = new()
        { Id = Guid.NewGuid(), EntityId = secondary.Id, RelatedEntityId = other.Id, RelationshipTypeId = typeId, EntityType = EntityType.Person };

        await _context.Contacts!.AddRangeAsync(primary, secondary, other);
        await _context.Set<Relationship>().AddRangeAsync(rel1, rel2);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<Relationship> rels = await _context.Set<Relationship>().Where(r => r.EntityId == primary.Id).ToListAsync();
        Assert.Single(rels);
        Assert.Equal(other.Id, rels.First().RelatedEntityId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenBothContactsHaveNullFirstNameResultIsEmptyString()
    {
        // FirstName is [Required] in the DB, so whitespace-only stands in for "blank" inputs.
        // MergeScalar treats whitespace-only as null; the ?? string.Empty fallback ensures
        // the result is "" rather than null.
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "   " };
        Contact secondary = new() { Id = Guid.NewGuid(), FirstName = "   " };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Empty(merged.FirstName);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenRelatedToSecondaryWithCircularRelationshipDeduplicates()
    {
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new() { Id = Guid.NewGuid(), FirstName = "Secondary" };
        Contact tertiary = new() { Id = Guid.NewGuid(), FirstName = "Tertiary" };
        Guid typeId = Guid.NewGuid();

        Relationship primaryToTertiary = new()
        {
            Id = Guid.NewGuid(),
            EntityId = primary.Id,
            RelatedEntityId = tertiary.Id,
            RelationshipTypeId = typeId,
            EntityType = EntityType.Person
        };
        Relationship secondaryToTertiary = new()
        {
            Id = Guid.NewGuid(),
            EntityId = secondary.Id,
            RelatedEntityId = tertiary.Id,
            RelationshipTypeId = typeId,
            EntityType = EntityType.Person
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary, tertiary);
        await _context.Set<Relationship>().AddRangeAsync(primaryToTertiary, secondaryToTertiary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<Relationship> relsFromPrimary = await _context.Set<Relationship>()
            .Where(r => r.EntityId == primary.Id)
            .ToListAsync();

        Assert.Single(relsFromPrimary);
        Assert.Equal(tertiary.Id, relsFromPrimary.First().RelatedEntityId);

        bool selfReferential = await _context.Set<Relationship>()
            .AnyAsync(r => r.EntityId == primary.Id && r.RelatedEntityId == primary.Id);
        Assert.False(selfReferential);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSecondaryPetContactSharesToPrimaryExistingPet()
    {
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new() { Id = Guid.NewGuid(), FirstName = "Secondary" };

        Pet sharedPet = new() { Id = Guid.NewGuid(), Name = "Buddy" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.Set<Pet>().AddAsync(sharedPet);
        await _context.Set<PetContact>().AddRangeAsync(
            new PetContact { PetId = sharedPet.Id, ContactId = primary.Id },
            new PetContact { PetId = sharedPet.Id, ContactId = secondary.Id });
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        List<PetContact> petContacts = await _context.Set<PetContact>()
            .Where(pc => pc.ContactId == primary.Id)
            .ToListAsync();

        Assert.Single(petContacts);
        Assert.Equal(sharedPet.Id, petContacts.First().PetId);

        int totalPetContacts = await _context.Set<PetContact>().CountAsync();
        Assert.Equal(1, totalPetContacts);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenHowWeMetFieldsSecondaryFillsBlankPrimary()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            HowWeMet = "Conference 2024",
            FirstMetOn = new DateOnly(2024, 6, 15)
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Conference 2024", merged.HowWeMet);
        Assert.Equal(new DateOnly(2024, 6, 15), merged.FirstMetOn);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenHowWeMetFieldsPrimaryWinsWhenBothSet()
    {
        Contact primary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Primary",
            HowWeMet = "Primary story",
            FirstMetOn = new DateOnly(2020, 1, 1)
        };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            HowWeMet = "Secondary story",
            FirstMetOn = new DateOnly(2024, 6, 15)
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal("Primary story", merged.HowWeMet);
        Assert.Equal(new DateOnly(2020, 1, 1), merged.FirstMetOn);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenIntroducedByOnSecondaryCarriesToPrimary()
    {
        Contact introducer = new() { Id = Guid.NewGuid(), FirstName = "Introducer" };
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            IntroducedByContactId = introducer.Id
        };

        await _context.Contacts!.AddRangeAsync(introducer, primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Equal(introducer.Id, merged.IntroducedByContactId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenPrimaryIntroducedBySecondaryClearsField()
    {
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new() { Id = Guid.NewGuid(), FirstName = "Secondary" };

        // Primary is introduced by the very contact being merged in. After the merge, this reference must
        // not point at the primary itself, and the dangling reference to the soon-to-be-deleted secondary
        // must be cleared explicitly so EF does not race the SetNull cascade.
        primary.IntroducedByContactId = secondary.Id;

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Null(merged.IntroducedByContactId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSecondaryIsIntroducerForOtherContactsRedirectsToPrimary()
    {
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new() { Id = Guid.NewGuid(), FirstName = "Secondary" };
        Contact dependent = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Dependent",
            IntroducedByContactId = secondary.Id
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary, dependent);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? after = await _context.Contacts.FindAsync(dependent.Id);
        Assert.NotNull(after);
        Assert.Equal(primary.Id, after.IntroducedByContactId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSecondaryIntroducedByPrimaryDoesNotCreateSelfReference()
    {
        Contact primary = new() { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            IntroducedByContactId = null
        };

        // Edge case: secondary's introducer is primary itself. Carrying that reference forward would make
        // primary its own introducer.
        secondary.IntroducedByContactId = primary.Id;

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.Null(merged.IntroducedByContactId);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenTransactionFailsBothContactsRemainIntact()
    {
        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Primary" };
        Contact secondary = new()
        { Id = Guid.NewGuid(), FirstName = "Secondary" };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        // Cause an exception during the merge by passing the same ID (or forcing a failure another way).
        // Actually, we need a failure AFTER the initial checks.
        // Let's create a scenario where saving fails, e.g., adding an entity with a duplicate key manually,
        // or just using a mock to force a failure.
        // Since we are using a real (InMemory) db context here, forcing a DbUpdateException is tricky without
        // doing something like violating a constraint. But in-memory doesn't enforce all constraints.
        // Let's create a Mock of IRepository that throws on UpdateAsync.
        Mock<IRepository> mockRepo = new();
        mockRepo.Setup(r => r.QueryUnfiltered<Contact>()).Returns(_context.Contacts);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PetContact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>())).ReturnsAsync([]);
        mockRepo.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Forced failure"));

        MergeService sutWithMockRepo = new(_context, mockRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sutWithMockRepo.MergeContactsAsync(primary.Id, secondary.Id));

        // Verify both contacts still exist in the database (since transaction would roll back, though InMemory doesn't truly support transactions,
        // the code should execute the rollback path without crashing).
        Contact? pCheck = await _context.Contacts.FindAsync(primary.Id);
        Contact? sCheck = await _context.Contacts.FindAsync(secondary.Id);

        Assert.NotNull(pCheck);
        Assert.NotNull(sCheck);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenSecondaryIsDeceasedPropagatesToPrimary()
    {
        // Deceased status is a one-way truth — merging a deceased duplicate into an
        // alive primary must mark the primary deceased and preserve the date of death,
        // otherwise reminders / dashboard / calendar would silently re-enable for a
        // person who has died.
        DateOnly dateOfDeath = new(2024, 6, 15);

        Contact primary = new()
        { Id = Guid.NewGuid(), FirstName = "Alive", LastName = "Record" };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Deceased",
            LastName = "Record",
            IsDeceased = true,
            DateOfDeath = dateOfDeath
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.True(merged.IsDeceased);
        Assert.Equal(dateOfDeath, merged.DateOfDeath);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenPrimaryIsDeceasedKeepsPrimaryDateOfDeath()
    {
        // When primary is deceased with a known date of death, prefer the primary's value.
        DateOnly primaryDod = new(2024, 6, 15);
        DateOnly secondaryDod = new(2023, 1, 1);

        Contact primary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Primary",
            IsDeceased = true,
            DateOfDeath = primaryDod
        };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            IsDeceased = true,
            DateOfDeath = secondaryDod
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.True(merged.IsDeceased);
        Assert.Equal(primaryDod, merged.DateOfDeath);
    }

    [Fact]
    public async Task MergeContactsAsyncWhenPrimaryIsDeceasedWithoutDateAdoptsSecondaryDate()
    {
        // If the primary is already marked deceased but has no date of death,
        // take the secondary's known date so the information isn't lost.
        DateOnly secondaryDod = new(2024, 3, 14);

        Contact primary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Primary",
            IsDeceased = true,
            DateOfDeath = null
        };
        Contact secondary = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Secondary",
            IsDeceased = true,
            DateOfDeath = secondaryDod
        };

        await _context.Contacts!.AddRangeAsync(primary, secondary);
        await _context.SaveChangesAsync();

        await _sut.MergeContactsAsync(primary.Id, secondary.Id);

        Contact? merged = await _context.Contacts.FindAsync(primary.Id);
        Assert.NotNull(merged);
        Assert.True(merged.IsDeceased);
        Assert.Equal(secondaryDod, merged.DateOfDeath);
    }
}
