using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class RelationshipServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly RelationshipService _service;

        public RelationshipServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new RelationshipService(_repositoryMock.Object);
        }

        [Fact]
        public async Task CreateRelationshipAsync_Forward_CorrectlySetsRelationship()
        {
            // Arrange
            var originalEntityId = Guid.NewGuid();
            var originalRelatedEntityId = Guid.NewGuid();
            var relationship = new Relationship
            {
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };
            var typeId = Guid.NewGuid();
            var selectedType = $"{typeId}_Fwd";

            // Act
            var result = await _service.CreateRelationshipAsync(relationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeId, relationship.RelationshipTypeId);
            Assert.Equal(originalEntityId, relationship.EntityId);
            Assert.Equal(originalRelatedEntityId, relationship.RelatedEntityId);
            Assert.Equal(originalEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRelationshipAsync_Reverse_CorrectlySwapsEntities()
        {
            // Arrange
            var originalEntityId = Guid.NewGuid();
            var originalRelatedEntityId = Guid.NewGuid();
            var relationship = new Relationship
            {
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };
            var typeId = Guid.NewGuid();
            var selectedType = $"{typeId}_Rev";

            // Act
            var result = await _service.CreateRelationshipAsync(relationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeId, relationship.RelationshipTypeId);

            // Check for swap
            Assert.Equal(originalRelatedEntityId, relationship.EntityId);
            Assert.Equal(originalEntityId, relationship.RelatedEntityId);

            // Result redirect ID should be the original EntityId (which is now RelatedEntityId)
            Assert.Equal(originalEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRelationshipAsync_MissingType_ReturnsFailure()
        {
            // Arrange
            var relationship = new Relationship();

            // Act
            var result = await _service.CreateRelationshipAsync(relationship, "");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsync_InvalidFormat_ReturnsFailure()
        {
            // Arrange
            var relationship = new Relationship();

            // Act
            var result = await _service.CreateRelationshipAsync(relationship, "InvalidFormat");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsync_InvalidGuid_ReturnsFailure()
        {
            // Arrange
            var relationship = new Relationship();

            // Act
            var result = await _service.CreateRelationshipAsync(relationship, "NotAGuid_Fwd");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task UpdateRelationshipAsync_Forward_CorrectlyUpdatesRelationship()
        {
            // Arrange
            var relationshipId = Guid.NewGuid();
            var originalEntityId = Guid.NewGuid();
            var originalRelatedEntityId = Guid.NewGuid();
            var existingRelationship = new Relationship
            {
                Id = relationshipId,
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };

            var newEntityId = Guid.NewGuid();
            var newRelatedEntityId = Guid.NewGuid();
            var updatedRelationship = new Relationship
            {
                EntityId = newEntityId,
                RelatedEntityId = newRelatedEntityId,
                EntityType = EntityTypes.Person,
                Description = "Updated Description"
            };

            var typeId = Guid.NewGuid();
            var selectedType = $"{typeId}_Fwd";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRelationship);

            // Act
            var result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeId, existingRelationship.RelationshipTypeId);
            Assert.Equal("Updated Description", existingRelationship.Description);
            Assert.Equal(newEntityId, existingRelationship.EntityId);
            Assert.Equal(newRelatedEntityId, existingRelationship.RelatedEntityId);
            Assert.Equal(newEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.UpdateAsync(existingRelationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_Reverse_CorrectlySwapsEntities()
        {
            // Arrange
            var relationshipId = Guid.NewGuid();
            var existingRelationship = new Relationship
            {
                Id = relationshipId,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person
            };

            var newEntityId = Guid.NewGuid();
            var newRelatedEntityId = Guid.NewGuid();
            var updatedRelationship = new Relationship
            {
                EntityId = newEntityId,
                RelatedEntityId = newRelatedEntityId,
                EntityType = EntityTypes.Person
            };

            var typeId = Guid.NewGuid();
            var selectedType = $"{typeId}_Rev";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRelationship);

            // Act
            var result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeId, existingRelationship.RelationshipTypeId);

            // Should be swapped
            Assert.Equal(newRelatedEntityId, existingRelationship.EntityId);
            Assert.Equal(newEntityId, existingRelationship.RelatedEntityId);

            // RedirectId should be original EntityId (now RelatedEntityId)
            Assert.Equal(newEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.UpdateAsync(existingRelationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_NotFound_ReturnsFailure()
        {
            // Arrange
            var relationshipId = Guid.NewGuid();
            var updatedRelationship = new Relationship();
            var typeId = Guid.NewGuid();
            var selectedType = $"{typeId}_Fwd";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Relationship?)null);

            // Act
            var result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship not found.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_MissingType_ReturnsFailure()
        {
            // Arrange
            var relationshipId = Guid.NewGuid();
            var updatedRelationship = new Relationship();

            // Act
            var result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_InvalidFormat_ReturnsFailure()
        {
            // Arrange
            var relationshipId = Guid.NewGuid();
            var updatedRelationship = new Relationship();

            // Act
            var result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "Invalid");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsync_Person_ReturnsContactsExcludingSelf_AndSorted()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var otherId1 = Guid.NewGuid();
            var otherId2 = Guid.NewGuid();

            var contact1 = new Contact { Id = otherId1, FirstName = "Zara", LastName = "Doe" }; // Should be last
            var contact2 = new Contact { Id = otherId2, FirstName = "Adam", LastName = "Smith" }; // Should be first

            var returnedList = new List<Contact> { contact1, contact2 };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(returnedList);

            // Act
            var result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Person, otherId1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(otherId2.ToString(), result[0].Value); // Adam
            Assert.Equal(otherId1.ToString(), result[1].Value); // Zara
            Assert.True(result[1].Selected); // otherId1 was selected

            // Verify the predicate logic
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync(
                It.Is<Expression<Func<Contact, bool>>>(expr => VerifyPredicate(expr, entityId)),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsync_Company_ReturnsEmployersExcludingSelf_AndSorted()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var otherId1 = Guid.NewGuid();
            var otherId2 = Guid.NewGuid();

            var emp1 = new Employer { Id = otherId1, CompanyName = "Z Corp" };
            var emp2 = new Employer { Id = otherId2, CompanyName = "A Inc" };

            var returnedList = new List<Employer> { emp1, emp2 };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Employer, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(returnedList);

            // Act
            var result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Company, otherId1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(otherId2.ToString(), result[0].Value); // A Inc
            Assert.Equal(otherId1.ToString(), result[1].Value); // Z Corp
            Assert.True(result[1].Selected); // otherId1 was selected

            // Verify the predicate logic
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync(
                It.Is<Expression<Func<Employer, bool>>>(expr => VerifyPredicate(expr, entityId)),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once);
        }

        private bool VerifyPredicate<T>(Expression<Func<T, bool>> expr, Guid entityId) where T : BaseEntity, new()
        {
            var func = expr.Compile();
            var shouldExclude = new T { Id = entityId };
            var shouldInclude = new T { Id = Guid.NewGuid() };

            return !func(shouldExclude) && func(shouldInclude);
        }

        [Fact]
        public void GetRelationshipTypeOptions_ReturnsCorrectOptions()
        {
            // Arrange
            // Using "Parent" which is asymmetric. ID: 7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a
            // "Parent" is Name, "Child" is OppositeName.
            var parentTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
            var selectedValue = $"{parentTypeId}_Rev"; // Should select "is Child of (Parent)"

            // Act
            var options = _service.GetRelationshipTypeOptions(EntityTypes.Person, selectedValue);

            // Assert
            Assert.NotEmpty(options);

            // Check for Parent (Fwd)
            var parentFwd = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Fwd");
            Assert.NotNull(parentFwd);
            Assert.Equal("is Parent of (Child)", parentFwd.Text);
            Assert.Equal("Family", parentFwd.Group);
            Assert.False(parentFwd.Selected);

            // Check for Parent (Rev) - Child
            var parentRev = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Rev");
            Assert.NotNull(parentRev);
            Assert.Equal("is Child of (Parent)", parentRev.Text);
            Assert.Equal("Family", parentRev.Group);
            Assert.True(parentRev.Selected);

            // Check for Spouse (Symmetric) - ID: b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c
            var spouseTypeId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

            var spouseFwd = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Fwd");
            Assert.NotNull(spouseFwd);
            Assert.Equal("is Spouse of", spouseFwd.Text); // Symmetric format

            var spouseRev = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Rev");
            Assert.Null(spouseRev); // Symmetric types shouldn't have Rev option
        }
    }
}
