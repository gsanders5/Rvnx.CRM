using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class SignificantDatesControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly SignificantDatesController _controller;

        public SignificantDatesControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);
            _controller = new SignificantDatesController(repository);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Create_WithValidData_ShouldCreateDate()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Anniversary",
                Date = DateTime.Today,
                EventFrequency = TimeSpan.FromDays(365)
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            SignificantDate? created = await _context.Set<SignificantDate>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Anniversary", created.Title);
        }

        [Fact]
        public async Task Create_WhenDuplicateBirthdayExists_ShouldReturnValidationError()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });

            // Existing Birthday
            _context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = Guid.NewGuid(),
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = SignificantDateTitles.Birthday,
                Date = new DateTime(1990, 1, 1),
                EventFrequency = TimeSpan.FromDays(365)
            });
            await _context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = SignificantDateTitles.Birthday, // Duplicate Title
                Date = DateTime.Today
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("Title"));
            Assert.Equal("A birthday is already set for this contact.", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);

            // Ensure no new date created
            Assert.Single(await _context.Set<SignificantDate>().ToListAsync());
        }

        [Fact]
        public async Task Create_WhenDuplicateBirthdayExistsWithDifferentCase_ShouldReturnValidationError()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });

            // Existing Birthday with lowercase 'birthday'
            _context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = Guid.NewGuid(),
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "birthday", // Lowercase
                Date = new DateTime(1990, 1, 1),
                EventFrequency = TimeSpan.FromDays(365)
            });
            await _context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = SignificantDateTitles.Birthday, // Uppercase (Standard)
                Date = DateTime.Today
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            // This expects failure (duplicate detection), but currently it will succeed because "birthday" != "Birthday"
            Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid, "Model state should be invalid due to duplicate birthday");
            Assert.True(_controller.ModelState.ContainsKey("Title"));
            Assert.Equal("A birthday is already set for this contact.", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldDeleteDate()
        {
            // Arrange
            Guid dateId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = dateId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Del",
                Date = DateTime.Today
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(dateId);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await _context.Set<SignificantDate>().FindAsync(dateId));
        }
    }
}
