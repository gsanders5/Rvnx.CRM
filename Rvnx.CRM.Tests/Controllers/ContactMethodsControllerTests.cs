using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class ContactMethodsControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly ContactMethodsController _controller;

        public ContactMethodsControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            _userMock.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, _userMock.Object);
            Repository repository = new(_context);

            _controller = new ContactMethodsController(repository)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreateGetReturnsViewWithCorrectModel()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = entityId, FirstName = "Parent" });
            await _context.SaveChangesAsync();

            string entityType = EntityTypes.Person;

            // Act
            IActionResult result = await _controller.Create(entityId, entityType);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactMethodFormDto model = Assert.IsType<ContactMethodFormDto>(viewResult.Model);
            Assert.Equal(entityId, model.EntityId);
            Assert.Equal(entityType, model.EntityType);
        }

        [Fact]
        public async Task CreatePostValidDataCreatesContactMethod()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            string entityType = EntityTypes.Person;
            // Create parent entity
            _context.Contacts.Add(new Contact { Id = entityId, FirstName = "Parent", LastName = "Entity" });
            await _context.SaveChangesAsync();

            ContactMethodFormDto dto = new()
            {
                EntityId = entityId,
                EntityType = entityType,
                Type = ContactMethodType.Phone,
                Value = "555-0199",
                Label = "Work"
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName); // RedirectToEntity -> Details
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(entityId, redirectResult.RouteValues?["id"]);

            ContactMethod? created = await _context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.Value == "555-0199");
            Assert.NotNull(created);
            Assert.Equal(entityId, created.ContactId);
            Assert.Equal(ContactMethodType.Phone, created.Type);
        }

        [Fact]
        public async Task CreatePostInvalidDataReturnsView()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = entityId, FirstName = "Parent" });
            await _context.SaveChangesAsync();

            ContactMethodFormDto dto = new()
            {
                EntityId = entityId,
                EntityType = EntityTypes.Person,
                // Missing Type and Value (required)
            };
            _controller.ModelState.AddModelError("Value", "Required");

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(dto, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task CreatePostWithNonExistentParentReturnsNotFound()
        {
            // Arrange
            // We do NOT create the parent entity
            Guid nonExistentEntityId = Guid.NewGuid();
            string entityType = EntityTypes.Person;

            ContactMethodFormDto dto = new()
            {
                EntityId = nonExistentEntityId,
                EntityType = entityType,
                Type = ContactMethodType.Email,
                Value = "orphan@example.com"
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);

            // Verify record does NOT exist in DB
            ContactMethod? created = await _context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.Value == "orphan@example.com");
            Assert.Null(created);
        }

        [Fact]
        public async Task EditPostValidDataUpdatesContactMethod()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            Guid methodId = Guid.NewGuid();

            // Seed
            _context.Contacts.Add(new Contact { Id = entityId, FirstName = "Test", LastName = "User" });
            _context.Set<ContactMethod>().Add(new ContactMethod
            {
                Id = methodId,
                ContactId = entityId,
                Type = ContactMethodType.Phone,
                Value = "Old Value",
                Label = "Old Label"
            });
            await _context.SaveChangesAsync();

            // Clear change tracker to ensure we are testing a fresh request
            _context.ChangeTracker.Clear();

            ContactMethodFormDto dto = new()
            {
                Id = methodId,
                EntityId = entityId,
                EntityType = EntityTypes.Person,
                Type = ContactMethodType.Email,
                Value = "New Value",
                Label = "New Label"
            };

            // Act
            IActionResult result = await _controller.Edit(methodId, dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            ContactMethod? updated = await _context.Set<ContactMethod>().FindAsync(methodId);
            Assert.NotNull(updated);
            Assert.Equal("New Value", updated.Value);
            Assert.Equal("New Label", updated.Label);
            Assert.Equal(ContactMethodType.Email, updated.Type);
        }

        [Fact]
        public async Task EditPostReturnsNotFoundWhenEntityDoesNotExist()
        {
            // Arrange
            Guid methodId = Guid.NewGuid();
            ContactMethodFormDto dto = new()
            {
                Id = methodId,
                EntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person,
                Type = ContactMethodType.Phone,
                Value = "Val"
            };

            // Act
            IActionResult result = await _controller.Edit(methodId, dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeletePostDeletesContactMethod()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            Guid methodId = Guid.NewGuid();

            _context.Contacts.Add(new Contact { Id = entityId, FirstName = "Test", LastName = "User" });
            _context.Set<ContactMethod>().Add(new ContactMethod
            {
                Id = methodId,
                ContactId = entityId,
                Type = ContactMethodType.Phone,
                Value = "To Delete"
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(methodId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            ContactMethod? deleted = await _context.Set<ContactMethod>().FindAsync(methodId);
            Assert.Null(deleted);
        }
    }
}
