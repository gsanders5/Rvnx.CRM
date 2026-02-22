using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
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

            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(_currentUserId);
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);
            _controller = new NotesController(repository);
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
            // Arrange
            Guid otherUserContactId = Guid.NewGuid();

            // Seed a contact belonging to ANOTHER user directly into the context
            // Note: Since we are adding directly to the context, we need to bypass the query filter or simulate the other user adding it.
            // But CRMDbContext applies filters on QUERY, not on direct DbSet access unless configured otherwise.
            // Wait, AddAsync uses the current user ID from the service if UserId is null.
            // So we must manually set UserId to _otherUserId.

            Contact otherUserContact = new()
            {
                Id = otherUserContactId,
                FirstName = "Private",
                UserId = _otherUserId
            };

            _context.Contacts.Add(otherUserContact);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Verify that the current user cannot see this contact via repository
            Repository repo = new(_context);
            bool canSee = await repo.ExistsAsync<Contact>(otherUserContactId);
            Assert.False(canSee, "Test setup failed: Current user should not see other user's contact.");

            NoteFormViewModel maliciousNote = new()
            {
                EntityId = otherUserContactId,
                EntityType = EntityTypes.Person,
                Title = "Malicious Note",
                Value = "I shouldn't be here"
            };

            // Act
            IActionResult result = await _controller.Create(maliciousNote);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateGetShouldReturnNotFoundWhenUserCannotAccessContact()
        {
            // Arrange
            Guid otherUserContactId = Guid.NewGuid();
            Contact otherUserContact = new()
            {
                Id = otherUserContactId,
                FirstName = "Private",
                UserId = _otherUserId
            };

            _context.Contacts.Add(otherUserContact);
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Create(otherUserContactId, EntityTypes.Person);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateGetShouldReturnBadRequestWhenEntityTypeIsNotPerson()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.Create(entityId, EntityTypes.Company);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Only Person entities are supported.", badRequest.Value);
        }
    }
}
