using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
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
