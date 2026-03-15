using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class FactsControllerTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly FactsController _controller;

    public FactsControllerTests()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
        _userMock.Setup(s => s.UserName).Returns("test-user");

        _context = new CRMDbContext(options, _userMock.Object);
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

        IActionResult result = await _controller.Create(entityId, EntityTypes.Person);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        FactFormDto model = Assert.IsType<FactFormDto>(viewResult.Model);
        Assert.Equal(entityId, model.EntityId);
    }

    [Fact]
    public async Task CreatePostValidDataCreatesFact()
    {
        Guid entityId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = entityId, FirstName = "Parent" });
        await _context.SaveChangesAsync();

        FactFormDto dto = new()
        {
            EntityId = entityId,
            EntityType = EntityTypes.Person,
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
            EntityId = entityId,
            EntityType = EntityTypes.Person,
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