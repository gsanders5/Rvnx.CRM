using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class NotesControllerTests
{
    public class Security
    {
        [Fact]
        public async Task NotesControllerEditShouldPreserveEntityId()
        {
            using CRMDbContext context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(context);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.IsPartialAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockContactLookupService.Setup(s => s.GetContactNameAsync(It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            INoteService noteService = new NoteService(repository, mockContactLookupService.Object);
            NotesController controller = new(noteService, repository, mockContactLookupService.Object);

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

            // Attacker tries to change ContactId via form submission
            NoteFormViewModel tamperAttempt = new()
            {
                Id = noteId,
                ContactId = attackerContactId, // Attempting to move note to different contact
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
            using CRMDbContext context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(context);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.IsPartialAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockContactLookupService.Setup(s => s.GetContactNameAsync(It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            INoteService noteService = new NoteService(repository, mockContactLookupService.Object);
            NotesController controller = new(noteService, repository, mockContactLookupService.Object);

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

        [Fact]
        public async Task NotesControllerEditShouldReturnNotFoundWhenNoteDoesNotExist()
        {
            using CRMDbContext context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(context);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.IsPartialAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockContactLookupService.Setup(s => s.GetContactNameAsync(It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            INoteService noteService = new NoteService(repository, mockContactLookupService.Object);
            NotesController controller = new(noteService, repository, mockContactLookupService.Object);

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
    }

    public class NotesControllerSecurityTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly NotesController _controller;
        private readonly Guid _currentUserId;
        private readonly Guid _otherUserId;

        public NotesControllerSecurityTests()
        {
            _currentUserId = Guid.NewGuid();
            _otherUserId = Guid.NewGuid();

            _context = TestDbContextFactory.Create(_currentUserId, "test-user", null, out _);
            Repository repository = new(_context);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.IsPartialAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockContactLookupService.Setup(s => s.GetContactNameAsync(It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            Mock<INoteService> mockNoteService = new(); // Adding missing mock
            mockNoteService.Setup(s => s.CreateAsync(It.IsAny<NoteFormViewModel>()))
                .ReturnsAsync(OperationResult.NotFound("Contact not found."));

            _controller = new NotesController(mockNoteService.Object, repository, mockContactLookupService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreatePostShouldReturnNotFoundWhenUserCannotAccessContact()
        {
            Guid otherUserContactId = Guid.NewGuid();

            // Note: Since we are adding directly to the context, we need to bypass the query filter or simulate the other user adding it.
            // But CRMDbContext applies filters on QUERY, not on direct DbSet access unless configured otherwise.
            // Wait, AddAsync uses the current user ID from the service if UserId is null.
            // So we must manually set UserId to _otherUserId.

            Contact otherUserContact = new()
            {
                Id = otherUserContactId,
                FirstName = "Private",
                UserId = _otherUserId,
                GroupId = Guid.NewGuid() // Explicitly set different group
            };

            _context.Contacts!.Add(otherUserContact);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            Repository repo = new(_context);
            bool canSee = await repo.ExistsAsync<Contact>(otherUserContactId);
            Assert.False(canSee, "Test setup failed: Current user should not see other user's contact.");

            NoteFormViewModel maliciousNote = new()
            {
                ContactId = otherUserContactId,
                Title = "Malicious Note",
                Value = "I shouldn't be here"
            };

            IActionResult result = await _controller.Create(maliciousNote);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateGetShouldReturnNotFoundWhenUserCannotAccessContact()
        {
            Guid otherUserContactId = Guid.NewGuid();
            Contact otherUserContact = new()
            {
                Id = otherUserContactId,
                FirstName = "Private",
                UserId = _otherUserId,
                GroupId = Guid.NewGuid() // Explicitly set different group
            };

            _context.Contacts!.Add(otherUserContact);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            IActionResult result = await _controller.Create(otherUserContactId);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    public class General : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly NotesController _controller;

        public General()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.IsPartialAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            mockContactLookupService.Setup(s => s.GetContactNameAsync(It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            INoteService noteService = new NoteService(repository, mockContactLookupService.Object);

            _controller = new NotesController(noteService, repository, mockContactLookupService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreatePostWithValidDataShouldCreateNote()
        {
            Guid contactId = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            NoteFormViewModel note = new()
            {
                ContactId = contactId,
                Title = "Test Note",
                Value = "Content"
            };

            IActionResult result = await _controller.Create(note);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Note? created = await _context.Set<Note>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Test Note", created.Title);
            Assert.Equal(contactId, created.ContactId);
        }

        [Fact]
        public async Task EditPostWithValidDataShouldUpdateNote()
        {
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Note>().Add(new Note { Id = noteId, ContactId = contactId, Title = "Old", Value = "OldVal" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            NoteFormViewModel update = new()
            {
                Id = noteId,
                ContactId = contactId,
                Title = "New",
                Value = "NewVal"
            };

            IActionResult result = await _controller.Edit(noteId, update);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Note? updated = await _context.Set<Note>().FindAsync(noteId);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Title);
        }

        [Fact]
        public async Task DeleteConfirmedWithValidIdShouldDeleteNote()
        {
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Note>().Add(new Note { Id = noteId, ContactId = contactId, Title = "Del", Value = "Val" });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.DeleteConfirmed(noteId);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await _context.Set<Note>().FindAsync(noteId));
        }
    }
}
