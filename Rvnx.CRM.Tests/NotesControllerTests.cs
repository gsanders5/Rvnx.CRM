using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class NotesControllerTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Create_Post_ShouldCreateNote()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            NotesController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

            Note note = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Test Note",
                Value = "Content"
            };

            // Act
            IActionResult result = await controller.Create(note);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Note? created = await context.Set<Note>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Test Note", created.Title);
            Assert.Equal(contactId, created.EntityId);
        }

        [Fact]
        public async Task Edit_Post_ShouldUpdateNote()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            NotesController controller = new(repository);

            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Note>().Add(new Note { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Old", Value = "OldVal" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            Note update = new()
            {
                Id = noteId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "New",
                Value = "NewVal"
            };

            // Act
            IActionResult result = await controller.Edit(noteId, update);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Note? updated = await context.Set<Note>().FindAsync(noteId);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.Title);
        }

        [Fact]
        public async Task Delete_Post_ShouldDeleteNote()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            NotesController controller = new(repository);

            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Note>().Add(new Note { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Del", Value = "Val" });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(noteId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await context.Set<Note>().FindAsync(noteId));
        }
    }
}
