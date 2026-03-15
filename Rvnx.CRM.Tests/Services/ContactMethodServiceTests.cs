using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactMethodServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactMethodService _service;

        public ContactMethodServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactMethodService(_repositoryMock.Object);
        }

        [Fact]
        public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenContactMethodExistsRethrows()
        {
            Guid contactMethodId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            ContactMethod existingContactMethod = new()
            { Id = contactMethodId, ContactId = contactId };
            ContactMethodFormDto dto = new()
            { Id = contactMethodId, EntityId = contactId, EntityType = EntityTypes.Person };

            _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContactMethod);

            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(contactMethodId, dto));
        }

        [Fact]
        public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenContactMethodDoesNotExistReturnsFailure()
        {
            Guid contactMethodId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            ContactMethod existingContactMethod = new()
            { Id = contactMethodId, ContactId = contactId };
            ContactMethodFormDto dto = new()
            { Id = contactMethodId, EntityId = contactId, EntityType = EntityTypes.Person };

            _repositoryMock.Setup(r => r.GetByIdAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContactMethod);

            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<ContactMethod>(contactMethodId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            OperationResult result = await _service.UpdateAsync(contactMethodId, dto);

            Assert.False(result.Success);
            Assert.Equal("Contact method not found.", result.ErrorMessage);
        }
    }
}