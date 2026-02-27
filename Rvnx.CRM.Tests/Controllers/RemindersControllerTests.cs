using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
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
            Repository repository = new(_context);

            Mock<IEntityService> mockEntityService = new();
            mockEntityService.Setup(s => s.IsPartialAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);
            mockEntityService.Setup(s => s.GetEntityNameAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync("Test Entity");

            IReminderService reminderService = new ReminderService(repository, mockEntityService.Object);

            _controller = new RemindersController(reminderService, repository, mockEntityService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreateGetWithValidIdsShouldReturnViewWithDefaultValues()
        {
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Create(contactId, EntityTypes.Person);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderFormViewModel? model = viewResult.Model as ReminderFormViewModel;
            Assert.NotNull(model);
            Assert.Equal(contactId, model.EntityId);
            Assert.Equal(EntityTypes.Person, model.EntityType);
            Assert.Equal(TimeSpan.FromDays(365), model.EventFrequency);
        }

        [Fact]
        public async Task CreateGetWhenEntityIdEmptyShouldReturnNotFound()
        {
            IActionResult result = await _controller.Create(Guid.Empty, EntityTypes.Person);

            Assert.IsType<NotFoundResult>(result);
        }


        [Fact]
        public async Task CreatePostWithValidDataShouldCreateReminder()
        {
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            ReminderFormViewModel dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Call John",
                Description = "Follow up on proposal",
                DueDate = DateTime.Now.AddDays(7),
                RemindMe = true,
                EventFrequency = TimeSpan.FromDays(30)
            };

            IActionResult result = await _controller.Create(dto);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Reminder? created = await _context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Call John", created.Title);
            Assert.Equal("Follow up on proposal", created.Description);
            Assert.Equal(contactId, created.ContactId);
            Assert.True(created.RemindMe);
            Assert.Equal(TimeSpan.FromDays(30), created.EventFrequency);
        }

        [Fact]
        public async Task EditGetWithValidIdShouldReturnViewWithReminder()
        {
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                ContactId = contactId,
                Title = "Test Reminder",
                DueDate = DateTime.Now.AddDays(5)
            });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Edit(reminderId);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderFormViewModel? model = viewResult.Model as ReminderFormViewModel;
            Assert.NotNull(model);
            Assert.Equal(reminderId, model.Id);
            Assert.Equal("Test Reminder", model.Title);
        }

        [Fact]
        public async Task EditGetWhenIdNullShouldReturnNotFound()
        {
            IActionResult result = await _controller.Edit(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditGetWhenReminderDoesNotExistShouldReturnNotFound()
        {
            IActionResult result = await _controller.Edit(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditPostWithValidDataShouldUpdateReminder()
        {
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                ContactId = contactId,
                Title = "Old Title",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            ReminderFormViewModel dto = new()
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

            IActionResult result = await _controller.Edit(reminderId, dto);

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
        public async Task EditPostWhenIdMismatchShouldReturnNotFound()
        {
            ReminderFormViewModel dto = new()
            {
                Id = Guid.NewGuid(),
                Title = "Test"
            };

            IActionResult result = await _controller.Edit(Guid.NewGuid(), dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteGetWithValidIdShouldReturnViewWithReminder()
        {
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                ContactId = contactId,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Delete(reminderId);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ReminderDeleteViewModel? model = viewResult.Model as ReminderDeleteViewModel;
            Assert.NotNull(model);
            Assert.Equal("To Delete", model.Title);
        }

        [Fact]
        public async Task DeleteConfirmedWithValidIdShouldRemoveReminder()
        {
            Guid reminderId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Reminder>().Add(new Reminder
            {
                Id = reminderId,
                ContactId = contactId,
                Title = "To Delete",
                DueDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.DeleteConfirmed(reminderId);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Assert.Null(await _context.Set<Reminder>().FindAsync(reminderId));
        }

        [Fact]
        public async Task DeleteConfirmedWhenReminderNotFoundShouldRedirectToHome()
        {
            IActionResult result = await _controller.DeleteConfirmed(Guid.NewGuid());

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task CreatePostWithIsCompletedTrueShouldPreserveIsCompleted()
        {
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            ReminderFormViewModel dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Already Done",
                DueDate = DateTime.Now.AddDays(-1),
                IsCompleted = true
            };

            await _controller.Create(dto);

            Reminder? created = await _context.Set<Reminder>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.True(created.IsCompleted);
        }
    }
}
