using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Tests.Helpers;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class RelationshipsControllerTests
{
    public class RelationshipsControllerPerformanceTests
    {

        [Fact]
        public async Task CreateShouldUseSingleQueryForOptions()
        {
            Mock<IRepository> repositoryMock = new();
            Mock<IRelationshipService> relationshipServiceMock = new();

            // Setup mocks to return empty lists to avoid null ref in view model construction
            relationshipServiceMock.Setup(s => s.GetRelatedContactOptionsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
                .ReturnsAsync([]);
            relationshipServiceMock.Setup(s => s.GetRelationshipTypeOptions(It.IsAny<string?>()))
                .Returns([]);
            relationshipServiceMock.Setup(s => s.GetRelationshipTypes())
                .Returns([]);

            repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), default))
                .ReturnsAsync(1); // IsValidContactAsync

            repositoryMock.Setup(r => r.ListProjectedAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                    default))
                .ReturnsAsync(["Test Contact"]);

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            Mock<IRelationshipSuggestionService> suggestionServiceMock = new();
            RelationshipsController controller = new(relationshipServiceMock.Object, suggestionServiceMock.Object, repositoryMock.Object, mockContactLookupService.Object);

            Guid entityId = Guid.NewGuid();

            await controller.Create(entityId);

            relationshipServiceMock.Verify(s => s.GetRelatedContactOptionsAsync(entityId, null), Times.Once);
        }

    }
    public class RelationshipsControllerRedirectTests : IDisposable
    {

        private readonly CRMDbContext _context;
        private readonly RelationshipsController _controller;

        public RelationshipsControllerRedirectTests()
        {
            _context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(_context);
            RelationshipSuggestionService suggestionService = new(repository);
            RelationshipService relationshipService = new(repository, suggestionService);
            Mock<IUrlHelper> mockUrlHelper = new();
            mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns((string url) => url.StartsWith('/'));

            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            _controller = new RelationshipsController(relationshipService, suggestionService, repository, mockContactLookupService.Object)
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
        public async Task DeleteConfirmedWithValidReturnUrlShouldRedirectToUrl()
        {
            Guid relId = Guid.NewGuid();
            _context.Relationships!.Add(new Core.Models.Contact.Relationship
            {
                Id = relId,
                ContactId = Guid.NewGuid(),
                RelatedContactId = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            string returnUrl = "/local/path";

            IActionResult result = await _controller.DeleteConfirmed(relId, returnUrl);

            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task DeleteConfirmedWithInvalidReturnUrlShouldRedirectToEntity()
        {
            Guid relId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            _context.Relationships!.Add(new Core.Models.Contact.Relationship
            {
                Id = relId,
                ContactId = entityId,
                RelatedContactId = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            string returnUrl = "http://malicious.com";

            IActionResult result = await _controller.DeleteConfirmed(relId, returnUrl);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(entityId, redirectResult.RouteValues?["id"]);
        }

    }
    public class General : IDisposable
    {

        private readonly CRMDbContext _context;
        private readonly RelationshipsController _controller;

        public General()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);
            RelationshipSuggestionService suggestionService = new(repository);
            RelationshipService relationshipService = new(repository, suggestionService);
            Mock<IContactLookupService> mockContactLookupService = new();
            mockContactLookupService.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            _controller = new RelationshipsController(relationshipService, suggestionService, repository, mockContactLookupService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task CreatePostWithForwardDirectionShouldCreateRelationship()
        {
            Guid p1Id = Guid.NewGuid();
            Guid p2Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1" });
            _context.Contacts!.Add(new Contact { Id = p2Id, FirstName = "P2" });
            await _context.SaveChangesAsync();

            Guid typeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"); // Parent

            string selection = $"{typeId}_Fwd";
            RelationshipFormViewModel viewModel = new()
            {
                ContactId = p1Id,
                RelatedContactId = p2Id,
                SelectedRelationshipType = selection
            };

            IActionResult result = await _controller.Create(viewModel);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Relationship? created = await _context.Set<Relationship>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(p1Id, created.ContactId);
            Assert.Equal(p2Id, created.RelatedContactId);
            Assert.Equal(typeId, created.RelationshipTypeId);
        }

        [Fact]
        public async Task CreatePostWithReverseDirectionShouldSwapEntitiesAndCreateRelationship()
        {
            Guid p1Id = Guid.NewGuid(); // User is on P1 page
            Guid p2Id = Guid.NewGuid(); // User selects P2 as related
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1" });
            _context.Contacts!.Add(new Contact { Id = p2Id, FirstName = "P2" });
            await _context.SaveChangesAsync();

            Guid typeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"); // Parent

            string selection = $"{typeId}_Rev";
            RelationshipFormViewModel viewModel = new()
            {
                ContactId = p1Id,
                RelatedContactId = p2Id,
                SelectedRelationshipType = selection
            };

            IActionResult result = await _controller.Create(viewModel);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Relationship? created = await _context.Set<Relationship>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(p2Id, created.ContactId);
            Assert.Equal(p1Id, created.RelatedContactId);
            Assert.Equal(typeId, created.RelationshipTypeId);
        }

        [Fact]
        public async Task DeleteConfirmedWithValidIdShouldDeleteRelationship()
        {
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            _context.Set<Relationship>().Add(new Relationship
            {
                Id = relId,
                ContactId = p1Id,
                RelatedContactId = Guid.NewGuid(),
                RelationshipTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a")
            });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.DeleteConfirmed(relId);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await _context.Set<Relationship>().FindAsync(relId));
        }

        [Fact]
        public async Task CreateGetShouldPopulateGroupedOptions()
        {
            Guid p1Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1" });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Create(p1Id);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            RelationshipFormViewModel viewModel = Assert.IsType<RelationshipFormViewModel>(viewResult.Model);

            IEnumerable<SelectOptionDto> options = viewModel.RelationshipTypeOptions;
            Assert.NotNull(options);

            Guid spouseId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");
            Assert.Contains(options,
                o => o.Value == $"{spouseId}_Fwd" && o.Text == "is Spouse of" && o.Group == "Romantic");

            // Father (Parent/Child is defined as Parent/Child in service, not Father/Child explicitly with that ID, but checking logic)
            Guid parentId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
            Assert.Contains(options, o => o.Value == $"{parentId}_Fwd" && o.Text == "is Parent of (Child)");
            Assert.Contains(options, o => o.Value == $"{parentId}_Rev" && o.Text == "is Child of (Parent)");
        }

        [Fact]
        public async Task CreatePostWhenRelationshipTypeSelectionIsEmptyShouldReturnViewWithPopulatedOptions()
        {
            Guid p1Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1", LastName = "User" });
            _context.Contacts!.Add(new Contact { Id = Guid.NewGuid(), FirstName = "P2" });
            await _context.SaveChangesAsync();

            RelationshipFormViewModel viewModel = new()
            {
                ContactId = p1Id,
                RelatedContactId = Guid.NewGuid(),
                SelectedRelationshipType = string.Empty
            };

            IActionResult result = await _controller.Create(viewModel);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            RelationshipFormViewModel resultViewModel = Assert.IsType<RelationshipFormViewModel>(viewResult.Model);

            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("SelectedRelationshipType"));

            // Verifying that options are repopulated so the View doesn't crash on render
            Assert.NotNull(resultViewModel.RelatedContactOptions);
            Assert.NotEmpty(resultViewModel.RelatedContactOptions);

            Assert.NotNull(resultViewModel.RelationshipTypeOptions);
            Assert.NotEmpty(resultViewModel.RelationshipTypeOptions);

            Assert.Equal(p1Id, resultViewModel.ContactId);
        }

        [Fact]
        public async Task CreatePartialWithValidDataCreatesRelationshipAndRedirects()
        {
            Guid p1Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1" });
            await _context.SaveChangesAsync();

            CreatePartialContactRelationshipDto dto = new()
            {
                PartialContactFirstName = "NewPartial",
                SelectedRelationshipType = $"{Guid.NewGuid()}_Fwd"
            };

            IActionResult result = await _controller.CreatePartial(p1Id, dto);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Relationship? created = await _context.Set<Relationship>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(p1Id, created.ContactId);
        }

        [Fact]
        public async Task PromoteWithValidContactIdReturnsRedirect()
        {
            Guid p1Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1", IsPartial = true });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Promote(p1Id);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
            Assert.Equal(p1Id, redirectResult.RouteValues?["id"]);

            Contact? promoted = await _context.Set<Contact>().FindAsync(p1Id);
            Assert.NotNull(promoted);
            Assert.False(promoted.IsPartial);
        }

        [Fact]
        public async Task EditPostWhenRelationshipTypeSelectionIsEmptyShouldReturnViewWithPopulatedOptions()
        {
            Guid relId = Guid.NewGuid();
            Guid p1Id = Guid.NewGuid();
            Guid p2Id = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = p1Id, FirstName = "P1" });
            _context.Contacts!.Add(new Contact { Id = p2Id, FirstName = "P2" });

            _context.Relationships!.Add(new Relationship
            {
                Id = relId,
                ContactId = p1Id,
                RelatedContactId = p2Id,
                RelationshipTypeId = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            RelationshipFormViewModel viewModel = new()
            {
                Id = relId,
                ContactId = p1Id,
                RelatedContactId = p2Id,
                SelectedRelationshipType = string.Empty // Simulate validation error
            };

            IActionResult result = await _controller.Edit(relId, viewModel);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            RelationshipFormViewModel resultViewModel = Assert.IsType<RelationshipFormViewModel>(viewResult.Model);

            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("SelectedRelationshipType"));

            // Verifying that options are repopulated so the View doesn't crash on render
            Assert.NotNull(resultViewModel.RelatedContactOptions);
            Assert.NotEmpty(resultViewModel.RelatedContactOptions);

            Assert.NotNull(resultViewModel.RelationshipTypeOptions);
            Assert.NotEmpty(resultViewModel.RelationshipTypeOptions);

            Assert.NotNull(resultViewModel.RelationshipTypes);
            Assert.NotEmpty(resultViewModel.RelationshipTypes);

            Assert.Equal(relId, resultViewModel.Id);
            Assert.Equal(p1Id, resultViewModel.ContactId);
        }

    }
}
