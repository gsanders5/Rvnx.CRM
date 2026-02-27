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
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            bool result = await _service.ExistsAsync(EntityTypes.Person, id);

            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Contact>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncNoteCallsRepositoryForNote()
        {
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            bool result = await _service.ExistsAsync(EntityTypes.Note, id);

            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Note>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncReminderCallsRepositoryForReminder()
        {
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            bool result = await _service.ExistsAsync(EntityTypes.Reminder, id);

            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Reminder>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncSignificantDateCallsRepositoryForSignificantDate()
        {
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            bool result = await _service.ExistsAsync(EntityTypes.SignificantDate, id);

            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<SignificantDate>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncRelationshipCallsRepositoryForRelationship()
        {
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            bool result = await _service.ExistsAsync(EntityTypes.Relationship, id);

            Assert.True(result);
            _repositoryMock.Verify(r => r.ExistsAsync<Relationship>(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExistsAsyncUnknownTypeReturnsFalse()
        {
            Guid id = Guid.NewGuid();

            bool result = await _service.ExistsAsync("UnknownType", id);

            Assert.False(result);
            Assert.Empty(_repositoryMock.Invocations);
        }

        [Fact]
        public async Task GetEntityNameAsyncPersonReturnsFullName()
        {
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, FirstName = "John", LastName = "Doe" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            string result = await _service.GetEntityNameAsync(EntityTypes.Person, id);

            Assert.Equal("John Doe", result);
        }

        [Fact]
        public async Task GetEntityNameAsyncCompanyReturnsCompanyName()
        {
            Guid id = Guid.NewGuid();
            Employer employer = new()
            { Id = id, CompanyName = "Acme Corp" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Employer>(id, It.IsAny<CancellationToken>())).ReturnsAsync(employer);

            string result = await _service.GetEntityNameAsync(EntityTypes.Company, id);

            Assert.Equal("Acme Corp", result);
        }

        [Fact]
        public async Task GetEntityNameAsyncUnknownTypeReturnsUnknownEntity()
        {
            Guid id = Guid.NewGuid();

            string result = await _service.GetEntityNameAsync("UnknownType", id);

            Assert.Equal("Unknown Entity", result);
        }

        [Fact]
        public async Task IsPartialAsyncPersonPartialReturnsTrue()
        {
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, IsPartial = true };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            bool result = await _service.IsPartialAsync(EntityTypes.Person, id);

            Assert.True(result);
        }

        [Fact]
        public async Task IsPartialAsyncPersonNotPartialReturnsFalse()
        {
            Guid id = Guid.NewGuid();
            Contact contact = new()
            { Id = id, IsPartial = false };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(id, It.IsAny<CancellationToken>())).ReturnsAsync(contact);

            bool result = await _service.IsPartialAsync(EntityTypes.Person, id);

            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialAsyncOtherTypeReturnsFalse()
        {
            Guid id = Guid.NewGuid();

            bool result = await _service.IsPartialAsync(EntityTypes.Note, id);

            Assert.False(result);
        }
    }
}
