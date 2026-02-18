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
    public class PetsControllerTests
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
        public void Create_Get_ShouldReturnViewWithDto()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid contactId = Guid.NewGuid();

            // Act
            IActionResult result = controller.Create(contactId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            CreatePetDto? model = viewResult.Model as CreatePetDto;
            Assert.NotNull(model);
            Assert.Equal(contactId, model.EntityId);
        }

        [Fact]
        public async Task Create_Post_ShouldCreatePet()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "John" });
            await context.SaveChangesAsync();

            CreatePetDto dto = new()
            {
                EntityId = contactId,
                Name = "Buddy",
                Species = "Dog",
                Breed = "Golden Retriever",
                Birthday = new DateTime(2020, 5, 15),
                Notes = "Loves to fetch"
            };

            // Act
            IActionResult result = await controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Pet? created = await context.Set<Pet>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal("Buddy", created.Name);
            Assert.Equal("Dog", created.Species);
            Assert.Equal("Golden Retriever", created.Breed);
            Assert.Equal(contactId, created.EntityId);
            Assert.Equal(EntityTypes.Person, created.EntityType);
        }

        [Fact]
        public async Task Edit_Get_ShouldReturnViewWithPetData()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "Whiskers",
                Species = "Cat",
                Breed = "Siamese"
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Edit(petId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            UpdatePetDto? model = viewResult.Model as UpdatePetDto;
            Assert.NotNull(model);
            Assert.Equal(petId, model.Id);
            Assert.Equal("Whiskers", model.Name);
            Assert.Equal("Cat", model.Species);
            Assert.Equal("Siamese", model.Breed);
            Assert.Equal(contactId, viewResult.ViewData["EntityId"]);
        }

        [Fact]
        public async Task Edit_Get_ShouldReturnNotFound_WhenPetDoesNotExist()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            // Act
            IActionResult result = await controller.Edit(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ShouldUpdatePet()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "Old Name",
                Species = "Dog"
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            UpdatePetDto dto = new()
            {
                Id = petId,
                Name = "New Name",
                Species = "Dog",
                Breed = "Labrador",
                Notes = "Updated notes"
            };

            // Act
            IActionResult result = await controller.Edit(petId, dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            Pet? updated = await context.Set<Pet>().FindAsync(petId);
            Assert.NotNull(updated);
            Assert.Equal("New Name", updated.Name);
            Assert.Equal("Labrador", updated.Breed);
            Assert.Equal("Updated notes", updated.Notes);
        }

        [Fact]
        public async Task Edit_Post_ShouldReturnNotFound_WhenIdMismatch()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            UpdatePetDto dto = new() { Id = Guid.NewGuid(), Name = "Test" };

            // Act
            IActionResult result = await controller.Edit(Guid.NewGuid(), dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ShouldReturnViewWithPet()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "ToDelete",
                Species = "Fish"
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Delete(petId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            PetDto? model = viewResult.Model as PetDto;
            Assert.NotNull(model);
            Assert.Equal("ToDelete", model.Name);
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldRemovePet()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid petId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            context.Set<Pet>().Add(new Pet
            {
                Id = petId,
                EntityId = contactId,
                EntityType = EntityTypes.Person,
                Name = "ToDelete"
            });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(petId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);

            Assert.Null(await context.Set<Pet>().FindAsync(petId));
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldRedirectToContacts_WhenPetNotFound()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            // Act
            IActionResult result = await controller.DeleteConfirmed(Guid.NewGuid());

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Contacts", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Create_Post_ShouldSetEntityTypeToPerson()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            PetsController controller = new(repository);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test" });
            await context.SaveChangesAsync();

            CreatePetDto dto = new()
            {
                EntityId = contactId,
                Name = "Test Pet",
                Species = "Bird"
            };

            // Act
            await controller.Create(dto);

            // Assert
            Pet? created = await context.Set<Pet>().FirstOrDefaultAsync();
            Assert.NotNull(created);
            Assert.Equal(EntityTypes.Person, created.EntityType);
        }
    }
}