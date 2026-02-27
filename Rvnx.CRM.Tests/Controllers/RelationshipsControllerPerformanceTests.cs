using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class RelationshipsControllerPerformanceTests
    {
        [Fact]
        public async Task CreateShouldUseSingleQueryForOptions()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<IRelationshipService> relationshipServiceMock = new();

            // Setup mocks to return empty lists to avoid null ref in view model construction
            relationshipServiceMock.Setup(s => s.GetRelatedEntityOptionsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>()))
                .ReturnsAsync([]);
            relationshipServiceMock.Setup(s => s.GetRelationshipTypeOptions(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns([]);
            relationshipServiceMock.Setup(s => s.GetRelationshipTypes(It.IsAny<string>()))
                .Returns([]);

            repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), default))
                .ReturnsAsync(1); // IsValidContactAsync

            // Mock ListProjectedAsync for GetEntityName
            repositoryMock.Setup(r => r.ListProjectedAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<Contact, string>>>(),
                    default))
                .ReturnsAsync(["Test Contact"]);

            RelationshipsController controller = new(relationshipServiceMock.Object, repositoryMock.Object);

            Guid entityId = Guid.NewGuid();

            // Act
            await controller.Create(entityId, EntityTypes.Person);

            // Assert
            // Verify that GetRelatedEntityOptionsAsync is called exactly once
            relationshipServiceMock.Verify(s => s.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Person, null), Times.Once);
        }
    }
}
