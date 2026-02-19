using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class RemindersControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly RemindersController _controller;

        public RemindersControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new Repository(_context);
            _controller = new RemindersController(repository);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Create_Get_WithValidIds_ShouldReturnViewWithDefaultValues()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Create(contactId, EntityTypes.Person);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal(contactId, model.EntityId);
            Assert.Equal(EntityTypes.Person, model.EntityType);
            Assert.Equal(TimeSpan.FromDays(365), model.EventFrequency);
        }

        [Fact]
        public async Task Create_Get_WhenEntityIdEmpty_ShouldReturnNotFound()
        {
            // Act
            IActionResult result = await _controller.Create(Guid.Empty, EntityTypes.Person);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_WhenEntityTypeEmpty_ShouldReturnNotFound()
        {
            // Act
            IActionResult result = await _controller.Create(Guid.NewGuid(), "");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Post_WithValidData_ShouldCreateReminder()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            ReminderDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Call John",
                Description = "Follow up on proposal",
                DueDate = DateTime.Now.AddDays(7),
                RemindMe = true,
                EventFrequency = TimeSpan.FromDays(30)
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Reminder? created = await _context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Call John", created.Title);
            Assert.Equal("Follow up on proposal", created.Description);
            Assert.Equal(contactId, created.EntityId);
            Assert.True(created.RemindMe);
            Assert.Equal(TimeSpan.FromDays(30), created.EventFrequency);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ShouldReturnViewWithReminder()
        {
            // Arrange
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Test Reminder",
                DueDate = DateTime.Now.AddDays(5)
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Edit(reminderId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal(reminderId, model.Id);
            Assert.Equal("Test Reminder", model.Title);
        }

        [Fact]
        public async Task Edit_Get_WhenIdNull_ShouldReturnNotFound()
        {
            // Act
            IActionResult result = await _controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WhenReminderDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            IActionResult result = await _controller.Edit(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_WithValidData_ShouldUpdateReminder()
        {
            // Arrange
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Old Title",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            ReminderDto dto = new()
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Updated Title",
                Description = "New description",
                DueDate = DateTime.Now.AddDays(10),
                IsCompleted = true,
                RemindMe = false,
                EventFrequency = TimeSpan.Zero
            };

            // Act
            IActionResult result = await _controller.Edit(reminderId, dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Reminder? updated = await _context.Set<Reminder>().FindAsync(reminderId);
            Assert.NotNull(updated);
            Assert.Equal("Updated Title", updated.Title);
            Assert.Equal("New description", updated.Description);
            Assert.True(updated.IsCompleted);
            Assert.False(updated.RemindMe);
        }

        [Fact]
        public async Task Edit_Post_WhenIdMismatch_ShouldReturnNotFound()
        {
            // Arrange
            ReminderDto dto = new()
            {
                Id = Guid.NewGuid(),
                Title = "Test"
            };

            // Act
            IActionResult result = await _controller.Edit(Guid.NewGuid(), dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ShouldReturnViewWithReminder()
        {
            // Arrange
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Delete(reminderId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal("To Delete", model.Title);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_ShouldRemoveReminder()
        {
            // Arrange
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(reminderId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Assert.Null(await _context.Set<Reminder>().FindAsync(reminderId));
        }

        [Fact]
        public async Task DeleteConfirmed_WhenReminderNotFound_ShouldRedirectToHome()
        {
            // Act
            IActionResult result = await _controller.DeleteConfirmed(Guid.NewGuid());

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Create_Post_WithIsCompletedTrue_ShouldPreserveIsCompleted()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            ReminderDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Already Done",
                DueDate = DateTime.Now.AddDays(-1),
                IsCompleted = true
            };

            // Act
            await _controller.Create(dto);

            // Assert
            Reminder? created = await _context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.True(created.IsCompleted);
        }
    }
}