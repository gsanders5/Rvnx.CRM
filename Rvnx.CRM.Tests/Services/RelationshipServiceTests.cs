using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
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
        public async Task CreateRelationshipAsyncForwardCorrectlySetsRelationship()
        {
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

            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

            Assert.True(result.Success);
            Assert.Equal(typeId, relationship.RelationshipTypeId);
            Assert.Equal(originalEntityId, relationship.EntityId);
            Assert.Equal(originalRelatedEntityId, relationship.RelatedEntityId);
            Assert.Equal(originalEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRelationshipAsyncReverseCorrectlySwapsEntities()
        {
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

            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, selectedType);

            Assert.True(result.Success);
            Assert.Equal(typeId, relationship.RelationshipTypeId);

            Assert.Equal(originalRelatedEntityId, relationship.EntityId);
            Assert.Equal(originalEntityId, relationship.RelatedEntityId);

            // Result redirect ID should be the original EntityId (which is now RelatedEntityId)
            Assert.Equal(originalEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(relationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRelationshipAsyncMissingTypeReturnsFailure()
        {
            Relationship relationship = new();

            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "");

            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsyncInvalidFormatReturnsFailure()
        {
            Relationship relationship = new();

            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "InvalidFormat");

            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateRelationshipAsyncInvalidGuidReturnsFailure()
        {
            Relationship relationship = new();

            RelationshipOperationResult result = await _service.CreateRelationshipAsync(relationship, "NotAGuid_Fwd");

            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task UpdateRelationshipAsyncForwardCorrectlyUpdatesRelationship()
        {
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

            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

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
        public async Task UpdateRelationshipAsyncReverseCorrectlySwapsEntities()
        {
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

            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            Assert.True(result.Success);
            Assert.Equal(typeId, existingRelationship.RelationshipTypeId);

            Assert.Equal(newRelatedEntityId, existingRelationship.EntityId);
            Assert.Equal(newEntityId, existingRelationship.RelatedEntityId);

            // RedirectId should be original EntityId (now RelatedEntityId)
            Assert.Equal(newEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.UpdateAsync(existingRelationship, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateRelationshipAsyncNotFoundReturnsFailure()
        {
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            _repositoryMock.Setup(r => r.GetByIdAsync<Relationship>(relationshipId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Relationship?)null);

            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, selectedType);

            Assert.False(result.Success);
            Assert.Equal("Relationship not found.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRelationshipAsyncMissingTypeReturnsFailure()
        {
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();

            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "");

            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateRelationshipAsyncInvalidFormatReturnsFailure()
        {
            Guid relationshipId = Guid.NewGuid();
            Relationship updatedRelationship = new();

            RelationshipOperationResult result = await _service.UpdateRelationshipAsync(relationshipId, updatedRelationship, "Invalid");

            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsyncPersonReturnsContactsExcludingSelfAndSorted()
        {
            Guid entityId = Guid.NewGuid();
            Guid otherId1 = Guid.NewGuid();
            Guid otherId2 = Guid.NewGuid();

            List<SelectOptionDto> returnedList = [
                new SelectOptionDto { Value = otherId2.ToString(), Text = "Adam Smith" },
                new SelectOptionDto { Value = otherId1.ToString(), Text = "Zara Doe", Selected = true }
            ];

            _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, SelectOptionDto>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedList);

            List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Person, otherId1);

            Assert.Equal(2, result.Count);
            Assert.Equal(otherId2.ToString(), result[0].Value); // Adam
            Assert.Equal(otherId1.ToString(), result[1].Value); // Zara
            Assert.True(result[1].Selected); // otherId1 was selected

            _repositoryMock.Verify(r => r.ListProjectedAsync(
                It.Is<Expression<Func<Contact, bool>>>(expr => VerifyPredicate(expr, entityId)),
                It.IsAny<Expression<Func<Contact, SelectOptionDto>>>(),
                It.IsAny<Expression<Func<Contact, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsyncCompanyReturnsEmployersExcludingSelfAndSorted()
        {
            Guid entityId = Guid.NewGuid();
            Guid otherId1 = Guid.NewGuid();
            Guid otherId2 = Guid.NewGuid();

            List<SelectOptionDto> returnedList = [
                new SelectOptionDto { Value = otherId2.ToString(), Text = "A Inc" },
                new SelectOptionDto { Value = otherId1.ToString(), Text = "Z Corp", Selected = true }
            ];

            _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Employer, bool>>>(),
                It.IsAny<Expression<Func<Employer, SelectOptionDto>>>(),
                It.IsAny<Expression<Func<Employer, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedList);

            List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, EntityTypes.Company, otherId1);

            Assert.Equal(2, result.Count);
            Assert.Equal(otherId2.ToString(), result[0].Value); // A Inc
            Assert.Equal(otherId1.ToString(), result[1].Value); // Z Corp
            Assert.True(result[1].Selected); // otherId1 was selected

            _repositoryMock.Verify(r => r.ListProjectedAsync(
                It.Is<Expression<Func<Employer, bool>>>(expr => VerifyPredicate(expr, entityId)),
                It.IsAny<Expression<Func<Employer, SelectOptionDto>>>(),
                It.IsAny<Expression<Func<Employer, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static bool VerifyPredicate<T>(Expression<Func<T, bool>> expr, Guid entityId) where T : BaseEntity, new()
        {
            Func<T, bool> func = expr.Compile();
            T shouldExclude = new() { Id = entityId };
            T shouldInclude = new() { Id = Guid.NewGuid() };

            return !func(shouldExclude) && func(shouldInclude);
        }

        [Fact]
        public void GetRelationshipTypeOptionsReturnsCorrectOptions()
        {
            // Using "Parent" which is asymmetric. ID: 7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a
            Guid parentTypeId = Guid.Parse("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a");
            string selectedValue = $"{parentTypeId}_Rev"; // Should select "is Child of (Parent)"

            List<SelectOptionDto> options = _service.GetRelationshipTypeOptions(EntityTypes.Person, selectedValue);

            Assert.NotEmpty(options);

            SelectOptionDto? parentFwd = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Fwd");
            Assert.NotNull(parentFwd);
            Assert.Equal("is Parent of (Child)", parentFwd.Text);
            Assert.Equal("Family", parentFwd.Group);
            Assert.False(parentFwd.Selected);

            SelectOptionDto? parentRev = options.FirstOrDefault(o => o.Value == $"{parentTypeId}_Rev");
            Assert.NotNull(parentRev);
            Assert.Equal("is Child of (Parent)", parentRev.Text);
            Assert.Equal("Family", parentRev.Group);
            Assert.True(parentRev.Selected);

            Guid spouseTypeId = Guid.Parse("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c");

            SelectOptionDto? spouseFwd = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Fwd");
            Assert.NotNull(spouseFwd);
            Assert.Equal("is Spouse of", spouseFwd.Text); // Symmetric format

            SelectOptionDto? spouseRev = options.FirstOrDefault(o => o.Value == $"{spouseTypeId}_Rev");
            Assert.Null(spouseRev); // Symmetric types shouldn't have Rev option
        }

        [Fact]
        public async Task CreatePartialContactRelationshipAsyncCreatesContactAndRelationship()
        {
            Guid parentEntityId = Guid.NewGuid();
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            CreatePartialContactRelationshipDto dto = new()
            {
                PartialContactFirstName = "John",
                PartialContactLastName = "Doe",
                Description = "A partial contact"
            };

            RelationshipOperationResult result = await _service.CreatePartialContactRelationshipAsync(parentEntityId, selectedType, dto);

            Assert.True(result.Success);
            Assert.Equal(parentEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.IsPartial && c.FirstName == "John" && c.LastName == "Doe"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Relationship>(rel => rel.EntityId == parentEntityId && rel.RelationshipTypeId == typeId && rel.Description == "A partial contact"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // Save Changes
        }

        [Fact]
        public async Task CreatePartialContactRelationshipAsyncWithBirthdayAddsSignificantDate()
        {
            Guid parentEntityId = Guid.NewGuid();
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Fwd";

            DateTime birthday = new(1990, 1, 1);
            CreatePartialContactRelationshipDto dto = new()
            {
                PartialContactFirstName = "Jane",
                Birthday = birthday
            };

            RelationshipOperationResult result = await _service.CreatePartialContactRelationshipAsync(parentEntityId, selectedType, dto);

            Assert.True(result.Success);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd => sd.Title == SignificantDateTitles.Birthday && sd.Date == birthday && sd.RemindMe), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PromotePartialContactAsyncUpdatesIsPartial()
        {
            Guid contactId = Guid.NewGuid();
            Contact partialContact = new() { Id = contactId, IsPartial = true };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(partialContact);

            RelationshipOperationResult result = await _service.PromotePartialContactAsync(contactId);

            Assert.True(result.Success);
            Assert.Equal(contactId, result.RedirectId);
            Assert.False(partialContact.IsPartial);

            _repositoryMock.Verify(r => r.UpdateAsync(partialContact, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PromotePartialContactAsyncWhenNotPartialReturnsFailure()
        {
            Guid contactId = Guid.NewGuid();
            Contact fullContact = new() { Id = contactId, IsPartial = false };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fullContact);

            RelationshipOperationResult result = await _service.PromotePartialContactAsync(contactId);

            Assert.False(result.Success);
            Assert.Equal("Contact is not a partial contact.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreatePartialContactRelationshipAsyncReverseCorrectlySwapsEntities()
        {
            Guid parentEntityId = Guid.NewGuid();
            Guid typeId = Guid.NewGuid();
            string selectedType = $"{typeId}_Rev";

            CreatePartialContactRelationshipDto dto = new()
            {
                PartialContactFirstName = "John",
                PartialContactLastName = "Doe",
                Description = "A partial contact"
            };

            RelationshipOperationResult result = await _service.CreatePartialContactRelationshipAsync(parentEntityId, selectedType, dto);

            Assert.True(result.Success);
            Assert.Equal(parentEntityId, result.RedirectId);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.IsPartial && c.FirstName == "John" && c.LastName == "Doe"), It.IsAny<CancellationToken>()), Times.Once);

            // EntityId should be partialContact.Id (we don't know it exactly, but we can infer it's NOT parentEntityId)
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Relationship>(rel => rel.RelatedEntityId == parentEntityId && rel.RelationshipTypeId == typeId && rel.Description == "A partial contact"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePartialContactRelationshipAsyncMissingTypeReturnsFailure()
        {
            Guid parentEntityId = Guid.NewGuid();
            CreatePartialContactRelationshipDto dto = new();

            RelationshipOperationResult result = await _service.CreatePartialContactRelationshipAsync(parentEntityId, "", dto);

            Assert.False(result.Success);
            Assert.Equal("Relationship Type is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreatePartialContactRelationshipAsyncInvalidFormatReturnsFailure()
        {
            Guid parentEntityId = Guid.NewGuid();
            CreatePartialContactRelationshipDto dto = new();

            RelationshipOperationResult result = await _service.CreatePartialContactRelationshipAsync(parentEntityId, "InvalidFormat", dto);

            Assert.False(result.Success);
            Assert.Equal("Invalid Relationship Type.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetRelatedEntityOptionsAsyncUnknownTypeReturnsEmptyList()
        {
            Guid entityId = Guid.NewGuid();

            List<SelectOptionDto> result = await _service.GetRelatedEntityOptionsAsync(entityId, "UnknownType");

            Assert.Empty(result);
        }

        [Fact]
        public async Task PromotePartialContactAsyncNotFoundReturnsFailure()
        {
            Guid contactId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact?)null);

            RelationshipOperationResult result = await _service.PromotePartialContactAsync(contactId);

            Assert.False(result.Success);
            Assert.Equal("Contact not found.", result.ErrorMessage);
        }
    }
}
