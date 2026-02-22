using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Xunit;

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
        public async Task ExistsAsync_Person_CallsRepositoryForContact()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(EntityTypes.Person, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_Note_CallsRepositoryForNote()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(EntityTypes.Note, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_Reminder_CallsRepositoryForReminder()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(EntityTypes.Reminder, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_SignificantDate_CallsRepositoryForSignificantDate()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(EntityTypes.SignificantDate, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_Relationship_CallsRepositoryForRelationship()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(EntityTypes.Relationship, id);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_UnknownType_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _service.ExistsAsync("UnknownType", id);

            // Assert
            Assert.False(result);
            Assert.Empty(_repositoryMock.Invocations);
        }
    }
}
