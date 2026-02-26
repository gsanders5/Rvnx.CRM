using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class NotesControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly NotesController _controller;

        public NotesControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);

            Mock<IEntityService> mockEntityService = new();
            mockEntityService.Setup(s => s.IsPartialAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
            mockEntityService.Setup(s => s.GetEntityNameAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            INoteService noteService = new NoteService(repository, mockEntityService.Object);

            _controller = new NotesController(noteService, repository, mockEntityService.Object);
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
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            NoteFormViewModel note = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Test Note",
                Value = "Content"
            };

            // Act
            IActionResult result = await _controller.Create(note);

            // Assert
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
            // Arrange
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Note>().Add(new Note { Id = noteId, ContactId = contactId, Title = "Old", Value = "OldVal" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            NoteFormViewModel update = new()
            {
                Id = noteId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "New",
                Value = "NewVal"
            };

            // Act
            IActionResult result = await _controller.Edit(noteId, update);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Note? updated = await _context.Set<Note>().FindAsync(noteId);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Title);
        }

        [Fact]
        public async Task DeleteConfirmedWithValidIdShouldDeleteNote()
        {
            // Arrange
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Note>().Add(new Note { Id = noteId, ContactId = contactId, Title = "Del", Value = "Val" });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(noteId);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await _context.Set<Note>().FindAsync(noteId));
        }
    }
}
