using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class FactsControllerTests
{
    public class Security
    {
        [Fact]
        public async Task FactsControllerEditShouldPreserveEntityId()
        {
            using CRMDbContext context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(context);
            IFactService factService = new FactService(repository);
            FactsController controller = new(factService, repository);

            Guid factId = Guid.NewGuid();
            Guid originalContactId = Guid.NewGuid();
            Guid attackerContactId = Guid.NewGuid();

            context.Contacts!.Add(new Contact { Id = originalContactId, FirstName = "Original" });
            context.Contacts!.Add(new Contact { Id = attackerContactId, FirstName = "Attacker" });

            context.Set<Fact>().Add(new Fact
            {
                Id = factId,
                ContactId = originalContactId,
                Category = "Favorites",
                Value = "Blue"
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            FactFormDto tamperAttempt = new()
            {
                Id = factId,
                ContactId = attackerContactId,
                Category = "Updated Category",
                Value = "Updated Value"
            };

            await controller.Edit(factId, tamperAttempt);

            Fact? updatedFact = await context.Set<Fact>().FindAsync(factId);
            Assert.NotNull(updatedFact);
            Assert.Equal(originalContactId, updatedFact.ContactId);
            Assert.Equal("Updated Category", updatedFact.Category);
            Assert.Equal("Updated Value", updatedFact.Value);
        }

        [Fact]
        public async Task FactsControllerEditShouldReturnNotFoundWhenIdMismatch()
        {
            using CRMDbContext context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(context);
            IFactService factService = new FactService(repository);
            FactsController controller = new(factService, repository);

            Guid routeId = Guid.NewGuid();
            Guid bodyId = Guid.NewGuid();
            FactFormDto fact = new()
            {
                Id = bodyId,
                Category = "Test",
                Value = "Test"
            };

            IActionResult result = await controller.Edit(routeId, fact);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    public class General : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly FactsController _controller;

        public General()
        {
            _context = TestDbContextFactory.CreateForDefaultUser();
            Repository repository = new(_context);
            IFactService factService = new FactService(repository);

            _controller = new FactsController(factService, repository)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
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
        public async Task CreateGetReturnsViewWithCorrectModel()
        {
            Guid entityId = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = entityId, FirstName = "Parent" });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.Create(entityId);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            FactFormDto model = Assert.IsType<FactFormDto>(viewResult.Model);
            Assert.Equal(entityId, model.ContactId);
        }

        [Fact]
        public async Task CreatePostValidDataCreatesFact()
        {
            Guid entityId = Guid.NewGuid();
            _context.Contacts!.Add(new Contact { Id = entityId, FirstName = "Parent" });
            await _context.SaveChangesAsync();

            FactFormDto dto = new()
            {
                ContactId = entityId,
                Category = "Hobby",
                Value = "Reading"
            };

            IActionResult result = await _controller.Create(dto);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(entityId, redirectResult.RouteValues?["id"]);

            Fact? created = await _context.Set<Fact>().FirstOrDefaultAsync(f => f.Value == "Reading");
            Assert.NotNull(created);
            Assert.Equal(entityId, created.ContactId);
        }

        [Fact]
        public async Task EditPostValidDataUpdatesFact()
        {
            Guid entityId = Guid.NewGuid();
            Guid factId = Guid.NewGuid();

            _context.Contacts!.Add(new Contact { Id = entityId, FirstName = "Test" });
            _context.Set<Fact>().Add(new Fact
            {
                Id = factId,
                ContactId = entityId,
                Category = "Old",
                Value = "OldVal"
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            FactFormDto dto = new()
            {
                Id = factId,
                ContactId = entityId,
                Category = "New",
                Value = "NewVal"
            };

            IActionResult result = await _controller.Edit(factId, dto);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Fact? updated = await _context.Set<Fact>().FindAsync(factId);
            Assert.NotNull(updated);
            Assert.Equal("NewVal", updated.Value);
        }

        [Fact]
        public async Task DeletePostDeletesFact()
        {
            Guid entityId = Guid.NewGuid();
            Guid factId = Guid.NewGuid();

            _context.Contacts!.Add(new Contact { Id = entityId, FirstName = "Test" });
            _context.Set<Fact>().Add(new Fact { Id = factId, ContactId = entityId, Category = "C", Value = "V" });
            await _context.SaveChangesAsync();

            IActionResult result = await _controller.DeleteConfirmed(factId);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Fact? deleted = await _context.Set<Fact>().FindAsync(factId);
            Assert.Null(deleted);
        }
    }
}
