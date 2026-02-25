using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class RelationshipsControllerRedirectTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly RelationshipsController _controller;
        private readonly Mock<IUrlHelper> _urlHelperMock;

        public RelationshipsControllerRedirectTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);
            RelationshipService relationshipService = new(repository);
            _controller = new RelationshipsController(repository, relationshipService);

            // Mock UrlHelper
            _urlHelperMock = new Mock<IUrlHelper>();
            _controller.Url = _urlHelperMock.Object;
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task DeleteConfirmedWithReturnUrlShouldRedirectToReturnUrl()
        {
            // Arrange
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            string returnUrl = "/Contacts/Details/SomeId";

            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            _urlHelperMock.Setup(x => x.IsLocalUrl(returnUrl)).Returns(true);

            // Act
            IActionResult result = await _controller.DeleteConfirmed(relId, returnUrl);

            // Assert
            RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
            Assert.Null(await _context.Set<Relationship>().FindAsync(relId));
        }

        [Fact]
        public async Task DeleteConfirmedWithoutReturnUrlShouldRedirectToEntity()
        {
            // Arrange
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();

            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(relId, null);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);
            Assert.Null(await _context.Set<Relationship>().FindAsync(relId));
        }

        [Fact]
        public async Task DeleteConfirmedWithInvalidReturnUrlShouldRedirectToEntity()
        {
            // Arrange
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            string returnUrl = "http://malicious-site.com";

            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            _urlHelperMock.Setup(x => x.IsLocalUrl(returnUrl)).Returns(false);

            // Act
            IActionResult result = await _controller.DeleteConfirmed(relId, returnUrl);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);
            Assert.Null(await _context.Set<Relationship>().FindAsync(relId));
        }
    }
}
