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
    public class RemindersControllerTests
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
        public async Task Create_Get_ShouldReturnViewWithDefaultValues()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Create(contactId, EntityTypes.Person);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal(contactId, model.EntityId);
            Assert.Equal(EntityTypes.Person, model.EntityType);
            Assert.Equal(TimeSpan.FromDays(365), model.EventFrequency);
        }

        [Fact]
        public async Task Create_Get_ShouldReturnNotFound_WhenEntityIdEmpty()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            // Act
            IActionResult result = await controller.Create(Guid.Empty, EntityTypes.Person);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ShouldReturnNotFound_WhenEntityTypeEmpty()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            // Act
            IActionResult result = await controller.Create(Guid.NewGuid(), "");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Post_ShouldCreateReminder()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

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
            IActionResult result = await controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Reminder? created = await context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Call John", created.Title);
            Assert.Equal("Follow up on proposal", created.Description);
            Assert.Equal(contactId, created.EntityId);
            Assert.True(created.RemindMe);
            Assert.Equal(TimeSpan.FromDays(30), created.EventFrequency);
        }

        [Fact]
        public async Task Edit_Get_ShouldReturnViewWithReminder()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Test Reminder",
                DueDate = DateTime.Now.AddDays(5)
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Edit(reminderId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal(reminderId, model.Id);
            Assert.Equal("Test Reminder", model.Title);
        }

        [Fact]
        public async Task Edit_Get_ShouldReturnNotFound_WhenIdNull()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            // Act
            IActionResult result = await controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ShouldReturnNotFound_WhenReminderDoesNotExist()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            // Act
            IActionResult result = await controller.Edit(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ShouldUpdateReminder()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Old Title",
                DueDate = DateTime.Now
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

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
            IActionResult result = await controller.Edit(reminderId, dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Reminder? updated = await context.Set<Reminder>().FindAsync(reminderId);
            Assert.NotNull(updated);
            Assert.Equal("Updated Title", updated.Title);
            Assert.Equal("New description", updated.Description);
            Assert.True(updated.IsCompleted);
            Assert.False(updated.RemindMe);
        }

        [Fact]
        public async Task Edit_Post_ShouldReturnNotFound_WhenIdMismatch()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            ReminderDto dto = new()
            {
                Id = Guid.NewGuid(),
                Title = "Test"
            };

            // Act
            IActionResult result = await controller.Edit(Guid.NewGuid(), dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ShouldReturnViewWithReminder()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Delete(reminderId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDto? model = viewResult.Model as ReminderDto;
            Assert.NotNull(model);
            Assert.Equal("To Delete", model.Title);
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldRemoveReminder()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(reminderId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Assert.Null(await context.Set<Reminder>().FindAsync(reminderId));
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldRedirectToHome_WhenReminderNotFound()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            // Act
            IActionResult result = await controller.DeleteConfirmed(Guid.NewGuid());

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Create_Post_ShouldPreserveIsCompleted()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RemindersController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

            ReminderDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Already Done",
                DueDate = DateTime.Now.AddDays(-1),
                IsCompleted = true
            };

            // Act
            await controller.Create(dto);

            // Assert
            Reminder? created = await context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.True(created.IsCompleted);
        }
    }
}