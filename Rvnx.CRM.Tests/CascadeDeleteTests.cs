using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests
{
    public class CascadeDeleteTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new CRMDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Dependencies()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository(context);
            var logger = new Mock<ILogger<ContactsController>>();
            var controller = new ContactsController(repository, logger.Object);

            var contactId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "Test", LastName = "Delete" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            // Add Dependencies
            await repository.AddAsync(new Note { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "N", Value = "V" });
            await repository.AddAsync(new Reminder { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "R", DueDate = DateTime.Now });
            await repository.AddAsync(new ImportantDate { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "D", Date = DateTime.Now });
            await repository.AddAsync(new Address { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Street = "S" });
            await repository.AddAsync(new ContactInfo { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Type = Core.Enumerations.ContactInfoType.Email, Value = "e@e.com" });
            await repository.AddAsync(new Attachment { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, AttachmentType = "T", ContentType = "C" });

            await repository.SaveChangesAsync();

            // Act
            await controller.DeleteConfirmed(contactId);

            // Assert
            Assert.Null(await repository.GetByIdAsync<Contact>(contactId));
            Assert.Empty(await repository.ListAsync<Note>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Reminder>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<ImportantDate>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Address>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<ContactInfo>(x => x.EntityId == contactId));
            Assert.Empty(await repository.ListAsync<Attachment>(x => x.EntityId == contactId));
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Relationships()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository(context);
            var logger = new Mock<ILogger<ContactsController>>();
            var controller = new ContactsController(repository, logger.Object);

            var c1 = new Contact { Id = Guid.NewGuid(), FirstName = "C1" };
            var c2 = new Contact { Id = Guid.NewGuid(), FirstName = "C2" };
            await repository.AddAsync(c1);
            await repository.AddAsync(c2);

            var typeId = Guid.NewGuid(); // Fake Type
            await repository.AddAsync(new RelationshipType { Id = typeId, Name = "Rel", OppositeName = "RelOp", EntityType = EntityTypes.Person });

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
            var rels = await repository.ListAsync<Relationship>(r => r.EntityId == c1.Id || r.RelatedEntityId == c1.Id);
            Assert.Empty(rels);
        }
    }
}
