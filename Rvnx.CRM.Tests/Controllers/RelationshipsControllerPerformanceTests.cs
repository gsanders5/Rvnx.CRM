using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Controllers
{
    public class RelationshipsControllerPerformanceTests : IDisposable
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly Mock<IRelationshipService> _mockRelationshipService;
        private readonly RelationshipsController _controller;

        public RelationshipsControllerPerformanceTests()
        {
            _mockRepository = new Mock<IRepository>();
            _mockRelationshipService = new Mock<IRelationshipService>();
            _controller = new RelationshipsController(_mockRepository.Object, _mockRelationshipService.Object);
        }

        [Fact]
        public async Task DeleteFetchesContactsInSingleQuery()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            Guid relatedEntityId = Guid.NewGuid();

            Relationship relationship = new Relationship
            {
                Id = relationshipId,
                EntityId = entityId,
                RelatedEntityId = relatedEntityId,
                EntityType = EntityTypes.Person,
                RelationshipTypeId = Guid.NewGuid()
            };

            _mockRepository.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(relationship);

            var contacts = new List<Contact>
            {
                new Contact { Id = entityId, FirstName = "Entity" },
                new Contact { Id = relatedEntityId, FirstName = "RelatedEntity" }
            };

            _mockRepository.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            // Act
            var result = await _controller.Delete(relationshipId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RelationshipDeleteViewModel>(viewResult.Model);

            // Verify that ListAsync was called once
            _mockRepository.Verify(r => r.ListAsync(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()), Times.Once, "Should call ListAsync once to fetch contacts.");

            // Verify that GetByIdAsync<Contact> was NOT called
            _mockRepository.Verify(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never, "Should not call GetByIdAsync for contacts.");
        }

        public void Dispose()
        {
             _controller.Dispose();
             GC.SuppressFinalize(this);
        }
    }
}
