using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactLookupServiceGetPartialContactIdsAsyncTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactLookupService _service;

        public ContactLookupServiceGetPartialContactIdsAsyncTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactLookupService(_repositoryMock.Object);
        }

        [Fact]
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetPartialContactIdsAsync_WhenIdsEmpty_ReturnsEmptyHashSetAndDoesNotCallRepository()
        {
            // Act
            HashSet<Guid> result = await _service.GetPartialContactIdsAsync(new List<Guid>());

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _repositoryMock.Verify(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetPartialContactIdsAsync_WhenIdsProvided_ReturnsHashSetOfPartialIds()
        {
            // Arrange
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            List<Guid> ids = new() { id1, id2 };

            _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid> { id1 });

            // Act
            HashSet<Guid> result = await _service.GetPartialContactIdsAsync(ids);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(id1, result);
        }
    }
}
