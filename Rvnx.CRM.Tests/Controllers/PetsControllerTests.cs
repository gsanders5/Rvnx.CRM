using Microsoft.AspNetCore.Mvc;
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

public class PetsControllerTests : IDisposable
{
    private readonly CRMDbContext _context;
    private readonly PetsController _controller;
    private readonly Mock<IContactReadService> _mockContactReadService;

    public PetsControllerTests()
    {
        _context = TestDbContextFactory.CreateForDefaultUser();
        Repository repository = new(_context);
        IPetService petService = new PetService(repository);

        _mockContactReadService = new Mock<IContactReadService>();
        _mockContactReadService
            .Setup(s => s.GetContactNamesAsync(It.IsAny<bool>(), It.IsAny<IEnumerable<Guid>?>()))
            .ReturnsAsync([]);

        _controller = new PetsController(petService, repository, _mockContactReadService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateGetWithValidContactIdShouldReturnViewWithDto()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        await _context.SaveChangesAsync();

        IActionResult result = await _controller.Create(contactId);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        PetFormDto? model = viewResult.Model as PetFormDto;
        Assert.NotNull(model);
        Assert.Equal(contactId, model.EntityId);
        Assert.Contains(contactId, model.ContactIds);
    }

    [Fact]
    public async Task CreatePostWithValidDataShouldCreatePetAndPetContact()
    {
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "John" });
        await _context.SaveChangesAsync();

        PetFormDto dto = new()
        {
            EntityId = contactId,
            ContactIds = [contactId],
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            Birthday = new DateTime(2020, 5, 15),
            Notes = "Loves to fetch"
        };

        IActionResult result = await _controller.Create(dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Contacts", redirectResult.ControllerName);

        Pet? created = await _context.Set<Pet>().FirstOrDefaultAsync();
        Assert.NotNull(created);
        Assert.Equal("Buddy", created.Name);
        Assert.Equal("Dog", created.Species);
        Assert.Equal("Golden Retriever", created.Breed);

        PetContact? petContact = await _context.Set<PetContact>().FirstOrDefaultAsync();
        Assert.NotNull(petContact);
        Assert.Equal(created.Id, petContact.PetId);
        Assert.Equal(contactId, petContact.ContactId);
    }

    [Fact]
    public async Task EditGetWithValidIdShouldReturnViewWithPetData()
    {
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet
        {
            Id = petId,
            Name = "Whiskers",
            Species = "Cat",
            Breed = "Siamese"
        });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();

        IActionResult result = await _controller.Edit(petId);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        PetFormDto? model = viewResult.Model as PetFormDto;
        Assert.NotNull(model);
        Assert.Equal(petId, model.Id);
        Assert.Equal("Whiskers", model.Name);
        Assert.Equal("Cat", model.Species);
        Assert.Equal("Siamese", model.Breed);
        Assert.Equal(contactId, model.EntityId);
        Assert.Contains(contactId, model.ContactIds);
    }

    [Fact]
    public async Task EditGetWhenPetDoesNotExistShouldReturnNotFound()
    {
        IActionResult result = await _controller.Edit(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPostWithValidDataShouldUpdatePet()
    {
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet
        {
            Id = petId,
            Name = "Old Name",
            Species = "Dog"
        });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        PetFormDto dto = new()
        {
            Id = petId,
            EntityId = contactId,
            ContactIds = [contactId],
            Name = "New Name",
            Species = "Dog",
            Breed = "Labrador",
            Notes = "Updated notes"
        };

        IActionResult result = await _controller.Edit(petId, dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);

        Pet? updated = await _context.Set<Pet>().FindAsync(petId);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Labrador", updated.Breed);
        Assert.Equal("Updated notes", updated.Notes);
    }

    [Fact]
    public async Task EditPostWhenIdMismatchShouldReturnNotFound()
    {
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet
        {
            Id = petId,
            Name = "Existing Pet",
            Species = "Dog"
        });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        PetFormDto dto = new() { Id = Guid.NewGuid(), Name = "Test", EntityId = contactId };

        IActionResult result = await _controller.Edit(petId, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteGetWithValidIdShouldReturnViewWithPet()
    {
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet
        {
            Id = petId,
            Name = "ToDelete",
            Species = "Fish"
        });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();

        IActionResult result = await _controller.Delete(petId);

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        PetDto? model = viewResult.Model as PetDto;
        Assert.NotNull(model);
        Assert.Equal("ToDelete", model.Name);
    }

    [Fact]
    public async Task DeleteConfirmedWithValidIdShouldRemovePet()
    {
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet
        {
            Id = petId,
            Name = "ToDelete"
        });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();

        IActionResult result = await _controller.DeleteConfirmed(petId);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal("Contacts", redirectResult.ControllerName);

        Assert.Null(await _context.Set<Pet>().FindAsync(petId));
    }

    [Fact]
    public async Task DeleteConfirmedWhenPetNotFoundShouldRedirectToContacts()
    {
        IActionResult result = await _controller.DeleteConfirmed(Guid.NewGuid());

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Contacts", redirectResult.ControllerName);
    }

    [Fact]
    public async Task EditGetExcludesDeceasedContactsFromOwnerPicker()
    {
        // Editing a pet's owners is a forward-looking action (assigning ownership going
        // forward). Symmetric with ActivitiesController.Edit and PetsController.Create,
        // the picker must filter out deceased contacts so a user cannot accidentally
        // assign a new owner who has died — while still keeping any already-attached
        // deceased contact selectable via alwaysIncludeIds.
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet { Id = petId, Name = "Whiskers", Species = "Cat" });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();

        await _controller.Edit(petId);

        _mockContactReadService.Verify(
            s => s.GetContactNamesAsync(true, It.IsAny<IEnumerable<Guid>?>()),
            Times.Once);
    }

    [Fact]
    public async Task EditPostExcludesDeceasedContactsFromOwnerPickerOnRedisplay()
    {
        // When validation fails on POST and the form is re-rendered, the picker rebuilt
        // for the redisplayed view must use the same exclude-deceased filter as GET.
        Guid petId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test" });
        _context.Set<Pet>().Add(new Pet { Id = petId, Name = "Existing Pet", Species = "Dog" });
        _context.Set<PetContact>().Add(new PetContact { PetId = petId, ContactId = contactId });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Force the POST path that re-renders the view by submitting an invalid model.
        _controller.ModelState.AddModelError("Name", "required");
        PetFormDto dto = new() { Id = petId, EntityId = contactId, ContactIds = [contactId] };

        await _controller.Edit(petId, dto);

        _mockContactReadService.Verify(
            s => s.GetContactNamesAsync(true, It.IsAny<IEnumerable<Guid>?>()),
            Times.Once);
    }

}
