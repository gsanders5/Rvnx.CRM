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
            Guid originalEntityId = Guid.NewGuid();
            Guid originalRelatedEntityId = Guid.NewGuid();
            Relationship relationship = new()
            {
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

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
            Guid originalEntityId = Guid.NewGuid();
            Guid originalRelatedEntityId = Guid.NewGuid();
            Relationship relationship = new()
            {
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Rev";

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

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
            Relationship relationship = new();

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsync_InvalidFormat_ReturnsFailure()
        {
            // Arrange
            Relationship relationship = new();

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "InvalidFormat");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsync_InvalidGuid_ReturnsFailure()
        {
            // Arrange
            Relationship relationship = new();

            // Act
            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "NotAGuid_Fwd");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task UpdateRelationshipAsync_Forward_CorrectlyUpdatesRelationship()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Guid originalEntityId = Guid.NewGuid();
            Guid originalRelatedEntityId = Guid.NewGuid();
            Relationship existingRelationship = new()
            {
                Id = relationshipId,
                EntityId = originalEntityId,
                RelatedEntityId = originalRelatedEntityId,
                EntityType = EntityTypes.Person
            };

            Guid newEntityId = Guid.NewGuid();
            Guid newRelatedEntityId = Guid.NewGuid();
            Relationship updatedRelationship = new()
            {
                EntityId = newEntityId,
                RelatedEntityId = newRelatedEntityId,
                EntityType = EntityTypes.Person,
                Description = "Updated Description"
            };

            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRelationship);

            // Act
            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

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
            Guid relationshipId = Guid.NewGuid();
            Relationship existingRelationship = new()
            {
                Id = relationshipId,
                EntityId = Guid.NewGuid(),
                RelatedEntityId = Guid.NewGuid(),
                EntityType = EntityTypes.Person
            };

            Guid newEntityId = Guid.NewGuid();
            Guid newRelatedEntityId = Guid.NewGuid();
            Relationship updatedRelationship = new()
            {
                EntityId = newEntityId,
                RelatedEntityId = newRelatedEntityId,
                EntityType = EntityTypes.Person
            };

            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Rev";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRelationship);

            // Act
            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

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
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Relationship?) null);

            // Act
            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship not found.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_MissingType_ReturnsFailure()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();

            // Act
            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateRelationshipAsync_InvalidFormat_ReturnsFailure()
        {
            // Arrange
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();

            // Act
            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "Invalid");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsync_Person_ReturnsContactsExcludingSelf_AndSorted()
        {
            // Arrange
            Guid entityId = Guid.NewGuid();
            Guid otherId1 = Guid.NewGuid();
            Guid otherId2 = Guid.NewGuid();

            Contact contact1 = new() { Id = otherId1, FirstName = "Zara", LastName = "Doe" }; // Should be last
            Contact contact2 = new() { Id = otherId2, FirstName = "Adam", LastName = "Smith" }; // Should be first

            List<Contact> returnedList = new()
            { contact1, contact2 };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(returnedList);

            // Act
            List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Person, otherId1);

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
            Guid entityId = Guid.NewGuid();
            Guid otherId1 = Guid.NewGuid();
            Guid otherId2 = Guid.NewGuid();

            Employer emp1 = new() { Id = otherId1, CompanyName = "Z Corp" };
            Employer emp2 = new() { Id = otherId2, CompanyName = "A Inc" };

            List<Employer> returnedList = new()
            { emp1, emp2 };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync(
                It.IsAny<Expression<Func<Employer, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(returnedList);

            // Act
            List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Company, otherId1);

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
            Func<T, bool> func = expr.Compile();
            T shouldExclude = new() { Id = entityId };
            T shouldInclude = new() { Id = Guid.NewGuid() };

            return !func(shouldExclude) && func(shouldInclude);
        }

        [Fact]
        public void GetRelationshipTypeOptions_ReturnsCorrectOptions()
        {
            // Arrange
            // Using "Parent" which is asymmetric. ID: 7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a
            // "Parent" is Name, "Child" is OppositeName.
            Guid parentTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
            string selectedValue = $"{parentTypeId}_Rev"; // Should select "is Child of (Parent)"

            // Act
            List<SelectOptionDto> options = _service.GetRelationshipTypeOptions(EntityTypes.Person, selectedValue);

            // Assert
            Assert.NotEmpty(options);

            // Check for Parent (Fwd)
            SelectOptionDto? parentFwd = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Fwd");
            Assert.NotNull(parentFwd);
            Assert.Equal("is Parent of (Child)", parentFwd.Text);
            Assert.Equal("Family", parentFwd.Group);
            Assert.False(parentFwd.Selected);

            // Check for Parent (Rev) - Child
            SelectOptionDto? parentRev = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Rev");
            Assert.NotNull(parentRev);
            Assert.Equal("is Child of (Parent)", parentRev.Text);
            Assert.Equal("Family", parentRev.Group);
            Assert.True(parentRev.Selected);

            // Check for Spouse (Symmetric) - ID: b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c
            Guid spouseTypeId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

            SelectOptionDto? spouseFwd = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Fwd");
            Assert.NotNull(spouseFwd);
            Assert.Equal("is Spouse of", spouseFwd.Text); // Symmetric format

            SelectOptionDto? spouseRev = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Rev");
            Assert.Null(spouseRev); // Symmetric types shouldn't have Rev option
        }
    }
}
