using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;
using System.Linq.Expressions;
using Xunit;

namespace Rvnx.CRM.Tests.Controllers
{
    public class RepositoryControllerPerformanceTests
    {
        private sealed class TestRepositoryController(IRepository repository) : RepositoryController(repository)
        {
            public Task<bool> PublicIsValidContactAsync(Guid id)
            {
                return IsValidContactAsync(id);
            }
        }

        [Fact]
        public async Task IsValidContactAsyncShouldUseEfficientQuery()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Guid contactId = Guid.NewGuid();

            // Setup: CountAsync returns 1, meaning the contact exists and matches criteria
            repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            TestRepositoryController controller = new(repositoryMock.Object);

            // Act
            bool result = await controller.PublicIsValidContactAsync(contactId);

            // Assert
            Assert.True(result);

            // Verify GetByIdAsync is NOT called
            repositoryMock.Verify(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never, "GetByIdAsync should NOT be called in the optimized version");

            // Verify CountAsync IS called
            repositoryMock.Verify(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Once, "CountAsync should be called in the optimized version");
        }
    }
}
