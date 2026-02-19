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
    public class SignificantDatesControllerReproductionTests
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
        public async Task Create_Post_ShouldFail_WhenDuplicateBirthday_DifferentCase()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            SignificantDatesController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            // Existing Birthday with lowercase 'birthday'
            context.Set<SignificantDate>().Add(new SignificantDate
            {
                Id = Guid.NewGuid(),
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = "birthday", // Lowercase
                Date = new DateTime(1990, 1, 1),
                EventFrequency = TimeSpan.FromDays(365)
            });
            await context.SaveChangesAsync();

            SignificantDateDto dto = new()
            {
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Title = SignificantDateTitles.Birthday, // Uppercase (Standard)
                Date = DateTime.Today
            };

            // Act
            IActionResult result = await controller.Create(dto);

            // Assert
            // This expects failure (duplicate detection), but currently it will succeed because "birthday" != "Birthday"
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid, "Model state should be invalid due to duplicate birthday");
            Assert.True(controller.ModelState.ContainsKey("Title"));
            Assert.Equal("A birthday is already set for this contact.", controller.ModelState["Title"]!.Errors[0].ErrorMessage);
        }
    }
}
