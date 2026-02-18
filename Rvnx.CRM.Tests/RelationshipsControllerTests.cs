using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
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
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
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
            await context.SaveChangesAsync();

            // Use a real static ID from Service
            Guid typeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"); // Parent

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
            await context.SaveChangesAsync();

            // Use a real static ID from Service
            Guid typeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"); // Parent

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
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(relId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await context.Set<Relationship>().FindAsync(relId));
        }

        [Fact]
        public async Task Create_Get_PopulatesGroupedOptions()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            RelationshipsController controller = new(repository);

            Guid p1Id = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = p1Id, FirstName = "P1" });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Create(p1Id, EntityTypes.Person);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            List<SelectListItem>? options = viewResult.ViewData["RelationshipTypeSelection"] as List<SelectListItem>;
            Assert.NotNull(options);

            // Check that options from static service are present
            // Spouse
            var spouseId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");
            Assert.Contains(options, o => o.Value == $"{spouseId}_Fwd" && o.Text == "is Spouse of" && o.Group?.Name == "Family");

            // Father (Parent/Child is defined as Parent/Child in service, not Father/Child explicitly with that ID, but checking logic)
            // Let's check "Parent"
            var parentId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
            Assert.Contains(options, o => o.Value == $"{parentId}_Fwd" && o.Text == "is Parent of (Child)");
            Assert.Contains(options, o => o.Value == $"{parentId}_Rev" && o.Text == "is Child of (Parent)");
        }
    }
}
