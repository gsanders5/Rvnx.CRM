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
    public class SignificantDatesControllerTests
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
        public async Task Create_Post_ShouldCreateDate_WhenValid()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            SignificantDatesController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Anniversary",
                Date = DateTime.Today,
                EventFrequency = TimeSpan.FromDays(365)
            };

            // Act
            IActionResult result = await controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            SignificantDate? created = await context.Set<SignificantDate>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Anniversary", created.Title);
        }

        [Fact]
        public async Task Create_Post_ShouldFail_WhenDuplicateBirthday()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            SignificantDatesController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            // Existing Birthday
            context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = Guid.NewGuid(),
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Birthday",
                Date = new DateTime(1990, 1, 1),
                EventFrequency = TimeSpan.FromDays(365)
            });
            await context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Birthday", // Duplicate Title
                Date = DateTime.Today
            };

            // Act
            IActionResult result = await controller.Create(dto);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Title"));
            Assert.Equal("A birthday is already set for this contact.", controller.ModelState["Title"]!.Errors[0].ErrorMessage);

            // Ensure no new date created
            Assert.Single(await context.Set<SignificantDate>().ToListAsync());
        }

        [Fact]
        public async Task Delete_Post_ShouldDeleteDate()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            SignificantDatesController controller = new(repository);

            Guid dateId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = dateId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "Del",
                Date = DateTime.Today
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(dateId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await context.Set<SignificantDate>().FindAsync(dateId));
        }
    }
}
