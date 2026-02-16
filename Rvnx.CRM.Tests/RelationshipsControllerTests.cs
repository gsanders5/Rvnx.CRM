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
    public class RelationshipsControllerTests
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
        public async Task Create_Post_ShouldCreateRelationship_ForwardDirection()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RelationshipsController controller = new(repository);

            Guid p1Id = Guid.NewGuid();
            Guid p2Id = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = p1Id, FirstName = "P1" });
            context.Contacts.Add(new Contact { Id = p2Id, FirstName = "P2" });

            Guid typeId = Guid.NewGuid();
            context.Set<RelationshipType>().Add(new RelationshipType { Id = typeId, Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person });
            await context.SaveChangesAsync();

            Relationship rel = new()
            {
                EntityId = p1Id,
                RelatedEntityId = p2Id,
                EntityType = EntityTypes.Person
            };
            string selection = $"{typeId}_Fwd";

            // Act
            IActionResult result = await controller.Create(rel, selection);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Relationship? created = await context.Set<Relationship>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(p1Id, created.EntityId);
            Assert.Equal(p2Id, created.RelatedEntityId);
            Assert.Equal(typeId, created.RelationshipTypeId);
        }

        [Fact]
        public async Task Create_Post_ShouldCreateRelationship_ReverseDirection_SwapsEntities()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RelationshipsController controller = new(repository);

            Guid p1Id = Guid.NewGuid(); // User is on P1 page
            Guid p2Id = Guid.NewGuid(); // User selects P2 as related
            context.Contacts.Add(new Contact { Id = p1Id, FirstName = "P1" });
            context.Contacts.Add(new Contact { Id = p2Id, FirstName = "P2" });

            Guid typeId = Guid.NewGuid();
            context.Set<RelationshipType>().Add(new RelationshipType { Id = typeId, Name = "Parent", OppositeName = "Child", EntityType = EntityTypes.Person });
            await context.SaveChangesAsync();

            Relationship rel = new()
            {
                EntityId = p1Id,
                RelatedEntityId = p2Id,
                EntityType = EntityTypes.Person
            };
            string selection = $"{typeId}_Rev";

            // Act
            IActionResult result = await controller.Create(rel, selection);

            // Assert
            // Should redirect back to P1 (original EntityId)
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Relationship? created = await context.Set<Relationship>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            // Swapped
            Assert.Equal(p2Id, created.EntityId);
            Assert.Equal(p1Id, created.RelatedEntityId);
            Assert.Equal(typeId, created.RelationshipTypeId);
        }

        [Fact]
        public async Task Delete_Post_ShouldDeleteRelationship()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RelationshipsController controller = new(repository);

            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(relId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await context.Set<Relationship>().FindAsync(relId));
        }
    }
}
