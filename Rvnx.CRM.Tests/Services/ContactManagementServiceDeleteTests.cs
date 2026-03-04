using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using Xunit;

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
            // Arrange
            var contactId = Guid.NewGuid();
            var users = new List<Rvnx.CRM.Core.Models.User>
            {
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId }
            };

            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Relationship>());

            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _service.DeleteContactAsync(contactId);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Rvnx.CRM.Core.Models.User>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Rvnx.CRM.Core.Models.User>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
