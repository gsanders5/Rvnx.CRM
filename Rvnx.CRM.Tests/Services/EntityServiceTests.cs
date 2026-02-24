using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Tests.Services
{
    public class EntityServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly EntityService _service;

        public EntityServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new EntityService(_repositoryMock.Object);
        }

        [Fact]
        public async Task ExistsAsyncPersonCallsRepositoryForContact()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            bool result = await _service.ExistsAsync(EntityTypes.Person, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncNoteCallsRepositoryForNote()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            bool result = await _service.ExistsAsync(EntityTypes.Note, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncReminderCallsRepositoryForReminder()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            bool result = await _service.ExistsAsync(EntityTypes.Reminder, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncSignificantDateCallsRepositoryForSignificantDate()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            bool result = await _service.ExistsAsync(EntityTypes.SignificantDate, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncRelationshipCallsRepositoryForRelationship()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            bool result = await _service.ExistsAsync(EntityTypes.Relationship, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncUnknownTypeReturnsFalse()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            bool result = await _service.ExistsAsync("UnknownType", id);

            // Assert
            Assert.False(result);
            Assert.Empty(_repositoryMock.Invocations);
        }

        [Fact]
        public async Task GetEntityNameAsyncPersonReturnsFullName()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, FirstName = "John", LastName = "Doe" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            // Act
            string result = await _service.GetEntityNameAsync(EntityTypes.Person, id);

            // Assert
            Assert.Equal("John Doe", result);
        }

        [Fact]
        public async Task GetEntityNameAsyncCompanyReturnsCompanyName()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Employer employer = new()
            { Id = id, CompanyName = "Acme Corp" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Employer>(id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);

            // Act
            string result = await _service.GetEntityNameAsync(EntityTypes.Company, id);

            // Assert
            Assert.Equal("Acme Corp", result);
        }

        [Fact]
        public async Task GetEntityNameAsyncUnknownTypeReturnsUnknownEntity()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            string result = await _service.GetEntityNameAsync("UnknownType", id);

            // Assert
            Assert.Equal("Unknown Entity", result);
        }

        [Fact]
        public async Task IsPartialAsyncPersonPartialReturnsTrue()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, IsPartial = true };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            // Act
            bool result = await _service.IsPartialAsync(EntityTypes.Person, id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsPartialAsyncPersonNotPartialReturnsFalse()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, IsPartial = false };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            // Act
            bool result = await _service.IsPartialAsync(EntityTypes.Person, id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialAsyncOtherTypeReturnsFalse()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            bool result = await _service.IsPartialAsync(EntityTypes.Note, id);

            // Assert
            Assert.False(result);
        }
    }
}
