using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rvnx.CRM.Tests
{
    public class CascadeDeleteTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((Guid?) null);
            mockUserService.Setup(u => u.UserName).Returns("TestUser");

            CRMDbContext context = new(options, mockUserService.Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Dependencies()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns((Guid?) null);
            userMock.Setup(u => u.UserName).Returns("TestUser");

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, logger.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "Delete" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            // Add Dependencies
            await repository.AddAsync(new Note { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "N", Value = "V" });
            await repository.AddAsync(new Reminder { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "R", DueDate = DateTime.Now });
            await repository.AddAsync(new SignificantDate { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "D", Date = DateTime.Now });
            await repository.AddAsync(new Address { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Street = "S" });
            await repository.AddAsync(new ContactMethod { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Type = Core.Enumerations.ContactMethodType.Email, Value = "e@e.com" });
            await repository.AddAsync(new Attachment { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, AttachmentType = "T", ContentType = "C" });

            await repository.SaveChangesAsync();

            // Act
            await controller.DeleteConfirmed(contactId);

            // Assert
            Assert.Null(await repository.GetByIdAsync<Contact>(contactId));
            Assert.Empty(await repository.ListAsync<Note>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Reminder>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<SignificantDate>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Address>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<ContactMethod>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Attachment>(x => x.EntityId == contactId));
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Relationships()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns((Guid?) null);
            userMock.Setup(u => u.UserName).Returns("TestUser");

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, logger.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Contact c1 = new() { Id = Guid.NewGuid(), FirstName = "C1" };
            Contact c2 = new() { Id = Guid.NewGuid(), FirstName = "C2" };
            await repository.AddAsync(c1);
            await repository.AddAsync(c2);

            Guid typeId = Guid.NewGuid(); // Fake Type (doesn't matter if it exists in DB for this test as we don't enforce FK here)

            // C1 -> C2
            await repository.AddAsync(new Relationship { Id = Guid.NewGuid(), EntityId = c1.Id, RelatedEntityId = c2.Id, EntityType = EntityTypes.Person, RelationshipTypeId = typeId });
            // C2 -> C1
            await repository.AddAsync(new Relationship { Id = Guid.NewGuid(), EntityId = c2.Id, RelatedEntityId = c1.Id, EntityType = EntityTypes.Person, RelationshipTypeId = typeId });

            await repository.SaveChangesAsync();

            // Act - Delete C1
            await controller.DeleteConfirmed(c1.Id);

            // Assert
            Assert.Null(await repository.GetByIdAsync<Contact>(c1.Id));
            Assert.NotNull(await repository.GetByIdAsync<Contact>(c2.Id));

            // Should be no relationships involving C1
            List<Relationship> rels = await repository.ListAsync<Relationship>(r => r.EntityId == c1.Id || r.RelatedEntityId == c1.Id);
            Assert.Empty(rels);
        }
    }
}