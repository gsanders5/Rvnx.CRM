using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
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
    }
}
