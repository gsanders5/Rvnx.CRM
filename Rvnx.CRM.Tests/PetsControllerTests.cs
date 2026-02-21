using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Pet;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class PetsControllerTests : IDisposable
    {
        private readonly CRMDbContext _context;
        private readonly PetsController _controller;

        public PetsControllerTests()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            _context = new CRMDbContext(options, mockCurrentUserService.Object);
            Repository repository = new(_context);
            _controller = new PetsController(repository);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public void Create_Get_WithValidContactId_ShouldReturnViewWithDto()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();

            // Act
            IActionResult result = _controller.Create(contactId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            PetFormDto? model = viewResult.Model as PetFormDto;
            Assert.NotNull(model);
            Assert.Equal(contactId, model.EntityId);
        }

        [Fact]
        public async Task Create_Post_WithValidData_ShouldCreatePet()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "John" });
            await _context.SaveChangesAsync();

            PetFormDto dto = new()
            {
                EntityId = contactId,
                Name = "Buddy",
                Species = "Dog",
                Breed = "Golden Retriever",
                Birthday = new DateTime(2020, 5, 15),
                Notes = "Loves to fetch"
            };

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Pet? created = await _context.Set<Pet>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Buddy", created.Name);
            Assert.Equal("Dog", created.Species);
            Assert.Equal("Golden Retriever", created.Breed);
            Assert.Equal(contactId, created.EntityId);
            Assert.Equal(EntityTypes.Person, created.EntityType);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ShouldReturnViewWithPetData()
        {
            // Arrange
            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "Whiskers",
                Species = "Cat",
                Breed = "Siamese"
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Edit(petId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            PetFormDto? model = viewResult.Model as PetFormDto;
            Assert.NotNull(model);
            Assert.Equal(petId, model.Id);
            Assert.Equal("Whiskers", model.Name);
            Assert.Equal("Cat", model.Species);
            Assert.Equal("Siamese", model.Breed);
            Assert.Equal(contactId, viewResult.ViewData["EntityId"]);
        }

        [Fact]
        public async Task Edit_Get_WhenPetDoesNotExist_ShouldReturnNotFound()
        {
            // Act
            IActionResult result = await _controller.Edit(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_WithValidData_ShouldUpdatePet()
        {
            // Arrange
            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "Old Name",
                Species = "Dog"
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            PetFormDto dto = new()
            {
                Id = petId,
                EntityId = contactId,
                Name = "New Name",
                Species = "Dog",
                Breed = "Labrador",
                Notes = "Updated notes"
            };

            // Act
            IActionResult result = await _controller.Edit(petId, dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Pet? updated = await _context.Set<Pet>().FindAsync(petId);
            Assert.NotNull(updated);
            Assert.Equal("New Name", updated.Name);
            Assert.Equal("Labrador", updated.Breed);
            Assert.Equal("Updated notes", updated.Notes);
        }

        [Fact]
        public async Task Edit_Post_WhenIdMismatch_ShouldReturnNotFound()
        {
            // Arrange
            PetFormDto dto = new() { Id = Guid.NewGuid(), Name = "Test" };

            // Act
            IActionResult result = await _controller.Edit(Guid.NewGuid(), dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ShouldReturnViewWithPet()
        {
            // Arrange
            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "ToDelete",
                Species = "Fish"
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.Delete(petId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            PetDto? model = viewResult.Model as PetDto;
            Assert.NotNull(model);
            Assert.Equal("ToDelete", model.Name);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_ShouldRemovePet()
        {
            // Arrange
            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            _context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "ToDelete"
            });
            await _context.SaveChangesAsync();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(petId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Assert.Null(await _context.Set<Pet>().FindAsync(petId));
        }

        [Fact]
        public async Task DeleteConfirmed_WhenPetNotFound_ShouldRedirectToContacts()
        {
            // Act
            IActionResult result = await _controller.DeleteConfirmed(Guid.NewGuid());

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Create_Post_AlwaysSetsEntityTypeToPerson()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await _context.SaveChangesAsync();

            PetFormDto dto = new()
            {
                EntityId = contactId,
                Name = "Test Pet",
                Species = "Bird"
            };

            // Act
            await _controller.Create(dto);

            // Assert
            Pet? created = await _context.Set<Pet>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(EntityTypes.Person, created.EntityType);
        }
    }
}