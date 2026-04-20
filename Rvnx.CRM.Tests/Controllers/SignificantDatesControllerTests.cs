using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class SignificantDatesControllerTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly SignificantDatesController _controller;

    public SignificantDatesControllerTests()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockCurrentUserService = new();
        mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
        mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

        _context = new CRMDbContext(options, mockCurrentUserService.Object);
        Repository repository = new(_context);
        ISignificantDateService significantDateService = new SignificantDateService(repository);
        _controller = new SignificantDatesController(significantDateService, repository);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateWithValidDataShouldCreateDate()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        await _context.SaveChangesAsync();

        CreateSignificantDateRequest dto = new()
        {
            EntityId = contactId,
            Title = "Anniversary",
            EventDate = DateOnly.FromDateTime(DateTime.Today),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual
        };

        IActionResult result = await _controller.Create(dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        SignificantDate? created = await _context.Set<SignificantDate>().FirstOrDefaultAsync();
        Assert.NotNull(created);
        Assert.Equal("Anniversary", created.Title);
    }

    [Fact]
    public async Task CreateWhenDuplicateBirthdayExistsShouldReturnValidationError()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });

        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = new DateOnly(1990, 1, 1),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        CreateSignificantDateRequest dto = new()
        {
            EntityId = contactId,
            Title = SignificantDateTitles.Birthday, // Duplicate Title
            EventDate = DateOnly.FromDateTime(DateTime.Today)
        };

        IActionResult result = await _controller.Create(dto);

        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("Title"));
        Assert.Equal("A birthday is already set for this contact.", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);

        Assert.Single(await _context.Set<SignificantDate>().ToListAsync());
    }

    [Fact]
    public async Task CreateWhenDuplicateBirthdayExistsWithDifferentCaseShouldReturnValidationError()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });

        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = "birthday", // Lowercase
            EventDate = new DateOnly(1990, 1, 1),
            RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        CreateSignificantDateRequest dto = new()
        {
            EntityId = contactId,
            Title = SignificantDateTitles.Birthday, // Uppercase (Standard)
            EventDate = DateOnly.FromDateTime(DateTime.Today)
        };

        IActionResult result = await _controller.Create(dto);

        // This expects failure (duplicate detection), but currently it will succeed because "birthday" != "Birthday"
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid, "Model state should be invalid due to duplicate birthday");
        Assert.True(_controller.ModelState.ContainsKey("Title"));
        Assert.Equal("A birthday is already set for this contact.", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task EditWhenChangingToBirthdayButOneAlreadyExistsShouldReturnValidationError()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });

        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = new DateOnly(1990, 1, 1)
        });

        Guid anniversaryId = Guid.NewGuid();
        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = anniversaryId,
            ContactId = contactId,
            Title = "Anniversary",
            EventDate = new DateOnly(2010, 5, 5)
        });
        await _context.SaveChangesAsync();

        UpdateSignificantDateRequest dto = new()
        {
            Id = anniversaryId,
            EntityId = contactId,
            Title = SignificantDateTitles.Birthday, // Change Anniversary to Birthday
            EventDate = new DateOnly(2010, 5, 5)
        };

        IActionResult result = await _controller.Edit(anniversaryId, dto);

        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Equal("A birthday is already set for this contact.", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task EditWhenExistingIsBirthdayAndUpdatingShouldSucceed()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });

        Guid birthdayId = Guid.NewGuid();
        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = birthdayId,
            ContactId = contactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = new DateOnly(1990, 1, 1)
        });
        await _context.SaveChangesAsync();

        UpdateSignificantDateRequest dto = new()
        {
            Id = birthdayId,
            EntityId = contactId,
            Title = SignificantDateTitles.Birthday,
            EventDate = new DateOnly(1990, 1, 2) // Change date only
        };

        IActionResult result = await _controller.Edit(birthdayId, dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);

        SignificantDate? updated = await _context.Set<SignificantDate>().FindAsync(birthdayId);
        Assert.NotNull(updated);
        Assert.Equal(new DateOnly(1990, 1, 2), updated.EventDate);
    }

    [Fact]
    public async Task DeleteConfirmedShouldDeleteDate()
    {
        Guid dateId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Set<SignificantDate>().Add(new SignificantDate
        {
            Id = dateId,
            ContactId = contactId,
            Title = "Del",
            EventDate = DateOnly.FromDateTime(DateTime.Today)
        });
        await _context.SaveChangesAsync();

        IActionResult result = await _controller.DeleteConfirmed(dateId, contactId);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(await _context.Set<SignificantDate>().FindAsync(dateId));
    }
}