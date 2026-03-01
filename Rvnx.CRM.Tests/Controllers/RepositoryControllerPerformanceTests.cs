using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers.Base;
using System.Linq.Expressions;

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
            Mock<IRepository> repositoryMock = new();
            Guid contactId = Guid.NewGuid();

            repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            TestRepositoryController controller = new(repositoryMock.Object);

            bool result = await controller.PublicIsValidContactAsync(contactId);

            Assert.True(result);

            repositoryMock.Verify(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never, "GetByIdAsync should NOT be called in the optimized version");

            repositoryMock.Verify(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Once, "CountAsync should be called in the optimized version");
        }
    }
}
