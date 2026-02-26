using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class RelationshipsControllerSecurityTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly RelationshipsController _controller;
        private readonly Mock<IUrlHelper> _urlHelperMock;

        public RelationshipsControllerSecurityTests()
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
        public async Task DeleteGetWithInvalidReturnUrlShouldSanitizeViewModel()
        {
            // Arrange
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            string maliciousUrl = "javascript:alert(1)";

            _context.Set<Contact>().Add(new Contact { Id = p1Id, FirstName = "Test", LastName = "User" });
            Guid relatedId = Guid.NewGuid();
            _context.Set<Contact>().Add(new Contact { Id = relatedId, FirstName = "Related", LastName = "User" });

            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = relatedId,
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            _urlHelperMock.Setup(x => x.IsLocalUrl(maliciousUrl)).Returns(false);

            // Act
            IActionResult result = await _controller.Delete(relId, maliciousUrl);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            RelationshipDeleteViewModel viewModel = Assert.IsType<RelationshipDeleteViewModel>(viewResult.Model);
            Assert.Null(viewModel.ReturnUrl);
        }

        [Fact]
        public async Task DeleteGetWithValidReturnUrlShouldKeepUrlInViewModel()
        {
            // Arrange
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            string safeUrl = "/Contacts/Details/SomeId";

            _context.Set<Contact>().Add(new Contact { Id = p1Id, FirstName = "Test", LastName = "User" });
            Guid relatedId = Guid.NewGuid();
            _context.Set<Contact>().Add(new Contact { Id = relatedId, FirstName = "Related", LastName = "User" });

            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                EntityId = p1Id,
                RelatedEntityId = relatedId,
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            _urlHelperMock.Setup(x => x.IsLocalUrl(safeUrl)).Returns(true);

            // Act
            IActionResult result = await _controller.Delete(relId, safeUrl);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            RelationshipDeleteViewModel viewModel = Assert.IsType<RelationshipDeleteViewModel>(viewResult.Model);
            Assert.Equal(safeUrl, viewModel.ReturnUrl);
        }
    }
}
