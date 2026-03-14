using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactManagementServiceDeleteTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        public ContactManagementServiceDeleteTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task DeleteContactAsyncShouldNotUpdateUsersInLoop()
        {
            Guid contactId = Guid.NewGuid();
            List<Core.Models.User> users =
            [
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId }
            ];

            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            await _service.DeleteContactAsync(contactId);

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Rvnx.CRM.Core.Models.User>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Rvnx.CRM.Core.Models.User>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}