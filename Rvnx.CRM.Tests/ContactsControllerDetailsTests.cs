using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerDetailsTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns("test-user-id");
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Details_ShouldReturnViewWithMappedRelationships()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            ContactsController controller = new(repository, loggerMock.Object, userMock.Object);

            // Create Contacts
            Guid mainContactId = Guid.NewGuid();
            Guid relatedContactId = Guid.NewGuid();
            Guid sourceContactId = Guid.NewGuid();

            Contact mainContact = new() { Id = mainContactId, FirstName = "Main", LastName = "Person" };
            Contact relatedContact = new() { Id = relatedContactId, FirstName = "Related", LastName = "Person" };
            Contact sourceContact = new() { Id = sourceContactId, FirstName = "Source", LastName = "Person" };

            context.Contacts.AddRange(mainContact, relatedContact, sourceContact);

            // Use a real static ID from Service (Friend)
            Guid typeId = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d");

            // Create Relationships
            // 1. Main -> Related (Where Main is EntityId)
            Relationship rel1 = new()
            {
                Id = Guid.NewGuid(),
                EntityId = mainContactId,
                EntityType = EntityTypes.Person,
                RelatedEntityId = relatedContactId,
                RelationshipTypeId = typeId
            };

            // 2. Source -> Main (Where Main is RelatedEntityId)
            Relationship rel2 = new()
            {
                Id = Guid.NewGuid(),
                EntityId = sourceContactId,
                EntityType = EntityTypes.Person,
                RelatedEntityId = mainContactId,
                RelationshipTypeId = typeId
            };

            context.Set<Relationship>().AddRange(rel1, rel2);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear(); // Ensure fresh fetch

            // Act
            IActionResult result = await controller.Details(mainContactId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactDetailDto model = Assert.IsAssignableFrom<ContactDetailDto>(viewResult.Model);

            // Verify Relationships (where main is EntityId)
            Assert.NotEmpty(model.Relationships);
            RelationshipDto? dtoRel1 = model.Relationships.FirstOrDefault(r => r.Id == rel1.Id);
            Assert.NotNull(dtoRel1);
            Assert.Equal("Related Person", dtoRel1.RelatedEntityName); // Check if name is mapped correctly
            Assert.Equal("Friend", dtoRel1.RelationshipTypeName);

            // Verify RelatedTo (where main is RelatedEntityId)
            Assert.NotEmpty(model.RelatedTo);
            RelationshipDto? dtoRel2 = model.RelatedTo.FirstOrDefault(r => r.Id == rel2.Id);
            Assert.NotNull(dtoRel2);
            Assert.Equal("Source Person", dtoRel2.EntityName); // Check if name is mapped correctly
            Assert.Equal("Friend", dtoRel2.RelationshipTypeName);
        }
    }
}
