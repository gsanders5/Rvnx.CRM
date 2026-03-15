using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Security;

/// <summary>
/// Tests to verify that Edit actions properly protect audit fields
/// and entity ownership from tampering via form data.
/// </summary>
public class EntityStateTamperingTests
{
    private static CRMDbContext GetInMemoryDbContext()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockCurrentUserService = new();
        mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
        mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

        return new CRMDbContext(options, mockCurrentUserService.Object);
    }

    #region NotesController Tests

    [Fact]
    public async Task NotesControllerEditShouldPreserveEntityId()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);

        Mock<IEntityService> mockEntityService = new();
        mockEntityService.Setup(s => s.IsPartialAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        mockEntityService.Setup(s => s.GetEntityNameAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync("Test Entity");

        INoteService noteService = new NoteService(repository, mockEntityService.Object);
        NotesController controller = new(noteService, repository, mockEntityService.Object);

        Guid noteId = Guid.NewGuid();
        Guid originalContactId = Guid.NewGuid();
        Guid attackerContactId = Guid.NewGuid();

        context.Contacts!.Add(new Contact { Id = originalContactId, FirstName = "Original" });
        context.Contacts!.Add(new Contact { Id = attackerContactId, FirstName = "Attacker" });

        context.Set<Note>().Add(new Note
        {
            Id = noteId,
            ContactId = originalContactId,
            Title = "Original Title",
            Value = "Original Content"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Attacker tries to change EntityId via form submission
        NoteFormViewModel tamperAttempt = new()
        {
            Id = noteId,
            EntityId = attackerContactId, // Attempting to move note to different contact
            EntityType = EntityTypes.Company, // Attempting to change entity type
            Title = "Updated Title",
            Value = "Updated Content"
        };

        await controller.Edit(noteId, tamperAttempt);

        Note? updatedNote = await context.Set<Note>().FindAsync(noteId);
        Assert.NotNull(updatedNote);
        Assert.Equal(originalContactId, updatedNote.ContactId); // Should stay original
        Assert.Equal("Updated Title", updatedNote.Title); // Content should update
        Assert.Equal("Updated Content", updatedNote.Value); // Content should update
    }

    [Fact]
    public async Task NotesControllerEditShouldPreserveCreatedDateAndCreatedBy()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);

        Mock<IEntityService> mockEntityService = new();
        mockEntityService.Setup(s => s.IsPartialAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        mockEntityService.Setup(s => s.GetEntityNameAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync("Test Entity");

        INoteService noteService = new NoteService(repository, mockEntityService.Object);
        NotesController controller = new(noteService, repository, mockEntityService.Object);

        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();

        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });

        Note originalNote = new()
        {
            Id = noteId,
            ContactId = contactId,
            Title = "Original",
            Value = "Content"
            // We do not set CreatedDate/CreatedBy here because CRMDbContext 
        };
        context.Set<Note>().Add(originalNote);

        // 1. Save the original note. The Context will assign 'Now' and 'test-user'.
        await context.SaveChangesAsync();

        // 2. Capture the authoritative values that were just saved to the DB.
        DateTime assignedCreatedDate = originalNote.CreatedDate;
        string? assignedCreatedBy = originalNote.CreatedBy;

        // 3. Clear memory to simulate a fresh HTTP request.
        context.ChangeTracker.Clear();

        // 4. Attacker constructs a payload trying to overwrite the audit fields
        NoteFormViewModel tamperAttempt = new()
        {
            Id = noteId,
            Title = "Updated",
            Value = "Updated Content"
            // CreatedDate and CreatedBy are not on the DTO, effectively testing that they can't be bound
        };

        await controller.Edit(noteId, tamperAttempt);

        // 5. Clear memory again to force a fresh read from the DB.
        context.ChangeTracker.Clear();

        Note? updatedNote = await context.Set<Note>().FindAsync(noteId);
        Assert.NotNull(updatedNote);

        // These ensure the edit logic ignored the attacker's values
        Assert.Equal(assignedCreatedDate, updatedNote.CreatedDate);
        Assert.Equal(assignedCreatedBy, updatedNote.CreatedBy);

        // Sanity check: ensure the Title/Value DID update
        Assert.Equal("Updated", updatedNote.Title);
    }

    #endregion

    #region FactsController Tests

    [Fact]
    public async Task FactsControllerEditShouldPreserveEntityId()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        IFactService factService = new FactService(repository);
        FactsController controller = new(factService, repository);

        Guid factId = Guid.NewGuid();
        Guid originalContactId = Guid.NewGuid();
        Guid attackerContactId = Guid.NewGuid();

        context.Contacts!.Add(new Contact { Id = originalContactId, FirstName = "Original" });
        context.Contacts!.Add(new Contact { Id = attackerContactId, FirstName = "Attacker" });

        context.Set<Fact>().Add(new Fact
        {
            Id = factId,
            ContactId = originalContactId,
            Category = "Favorites",
            Value = "Blue"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        FactFormDto tamperAttempt = new()
        {
            Id = factId,
            EntityId = attackerContactId,
            EntityType = EntityTypes.Company,
            Category = "Updated Category",
            Value = "Updated Value"
        };

        await controller.Edit(factId, tamperAttempt);

        Fact? updatedFact = await context.Set<Fact>().FindAsync(factId);
        Assert.NotNull(updatedFact);
        Assert.Equal(originalContactId, updatedFact.ContactId);
        Assert.Equal("Updated Category", updatedFact.Category);
        Assert.Equal("Updated Value", updatedFact.Value);
    }

    #endregion

    #region ContactMethodsController Tests

    [Fact]
    public async Task ContactMethodsControllerEditShouldPreserveEntityId()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        IContactMethodService contactMethodService = new ContactMethodService(repository);
        ContactMethodsController controller = new(contactMethodService, repository);

        Guid contactMethodId = Guid.NewGuid();
        Guid originalContactId = Guid.NewGuid();
        Guid attackerContactId = Guid.NewGuid();

        context.Contacts!.Add(new Contact { Id = originalContactId, FirstName = "Original" });
        context.Contacts!.Add(new Contact { Id = attackerContactId, FirstName = "Attacker" });

        context.Set<ContactMethod>().Add(new ContactMethod
        {
            Id = contactMethodId,
            ContactId = originalContactId,
            Type = Core.Enumerations.ContactMethodType.Email,
            Value = "original@example.com",
            Label = "Work"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ContactMethodFormDto tamperAttempt = new()
        {
            Id = contactMethodId,
            EntityId = attackerContactId,
            EntityType = EntityTypes.Company,
            Type = Core.Enumerations.ContactMethodType.Email,
            Value = "updated@example.com",
            Label = "Personal"
        };

        await controller.Edit(contactMethodId, tamperAttempt);

        ContactMethod? updatedMethod = await context.Set<ContactMethod>().FindAsync(contactMethodId);
        Assert.NotNull(updatedMethod);
        Assert.Equal(originalContactId, updatedMethod.ContactId);
        Assert.Equal("updated@example.com", updatedMethod.Value);
        Assert.Equal("Personal", updatedMethod.Label);
    }

    #endregion

    #region General Security Tests

    [Fact]
    public async Task NotesControllerEditShouldReturnNotFoundWhenNoteDoesNotExist()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);

        Mock<IEntityService> mockEntityService = new();
        mockEntityService.Setup(s => s.IsPartialAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
        mockEntityService.Setup(s => s.GetEntityNameAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync("Test Entity");

        INoteService noteService = new NoteService(repository, mockEntityService.Object);
        NotesController controller = new(noteService, repository, mockEntityService.Object);

        Guid nonExistentId = Guid.NewGuid();
        NoteFormViewModel note = new()
        {
            Id = nonExistentId,
            Title = "Test",
            Value = "Test"
        };

        IActionResult result = await controller.Edit(nonExistentId, note);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task FactsControllerEditShouldReturnNotFoundWhenIdMismatch()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repository = new(context);
        IFactService factService = new FactService(repository);
        FactsController controller = new(factService, repository);

        Guid routeId = Guid.NewGuid();
        Guid bodyId = Guid.NewGuid();
        FactFormDto fact = new()
        {
            Id = bodyId,
            Category = "Test",
            Value = "Test"
        };

        IActionResult result = await controller.Edit(routeId, fact);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}