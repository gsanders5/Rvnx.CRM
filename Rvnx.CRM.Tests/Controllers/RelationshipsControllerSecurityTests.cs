using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
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
            Mock<IUrlHelper> mockUrlHelper = new();
            mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns((string url) => url.StartsWith('/'));

            _controller = new RelationshipsController(relationshipService, repository)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                Url = mockUrlHelper.Object
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
        public async Task DeleteGetWithUnvalidatedReturnUrlShouldSanitizeIt()
        {
            // We need a valid relationship ID to reach the logic
            Guid relId = Guid.NewGuid();
            _context.Relationships.Add(new Core.Models.Contact.Relationship
            {
                Id = relId,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person
            });
            await _context.SaveChangesAsync();

            string maliciousUrl = "http://malicious.com";

            IActionResult result = await _controller.Delete(relId, maliciousUrl);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            dynamic? model = viewResult.Model;
            // In Razor Pages/Views, we access properties. Here we check if ReturnUrl in model is null
            // Since deleteViewModel is strongly typed in controller, let's cast
            Assert.NotNull(model);
            if (model != null)
            {
                Assert.Null(model.ReturnUrl);
            }
        }
    }
}
