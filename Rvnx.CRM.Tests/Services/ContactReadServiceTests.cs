using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactReadServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactReadService _service;

        public ContactReadServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactReadService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetContactDetailsAsync_ReturnsNull_WhenContactNotFound()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Contact>());

            // Act
            var result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetContactDetailsAsync_ReturnsDto_WhenContactFound()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "Test", LastName = "User" };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(new List<Contact> { contact });

            // Setup related entities to return empty lists to avoid null reference if logic assumes lists
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), default))
                .ReturnsAsync(new List<Note>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(), default))
                .ReturnsAsync(new List<Reminder>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), default))
                .ReturnsAsync(new List<SignificantDate>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default))
                .ReturnsAsync(new List<Relationship>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), default))
                .ReturnsAsync(new List<Pet>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
                .ReturnsAsync(new List<ContactMethod>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), default))
                .ReturnsAsync(new List<Fact>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default))
                .ReturnsAsync(new List<Attachment>());

            // Act
            var result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contactId, result.Id);
            Assert.Equal("Test", result.FirstName);
        }

        [Fact]
        public async Task GetContactDetailsAsync_PopulatesRelationshipsCorrectly()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid relatedId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "Main" };
            var relatedContact = new Contact { Id = relatedId, FirstName = "Related" };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                 It.Is<Expression<Func<Contact, bool>>>(expr => expr.Compile().Invoke(contact)), // Matches ID check
                 It.IsAny<CancellationToken>(),
                 It.IsAny<string[]>()))
                 .ReturnsAsync(new List<Contact> { contact });

            // Mock Relationships query
            // Using known IDs from RelationshipTypeService
            // Friend: a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d
            // Colleague: 33333333-3333-3333-3333-333333333301
            var relationships = new List<Relationship>
            {
                new Relationship {
                    Id = Guid.NewGuid(),
                    EntityId = contactId,
                    RelatedEntityId = relatedId,
                    EntityType = EntityTypes.Person,
                    RelationshipTypeId = Guid.Parse("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d")
                },
                new Relationship {
                    Id = Guid.NewGuid(),
                    EntityId = relatedId,
                    RelatedEntityId = contactId,
                    EntityType = EntityTypes.Person,
                    RelationshipTypeId = Guid.Parse("33333333-3333-3333-3333-333333333301")
                }
            };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                default))
                .ReturnsAsync(relationships);

            // Mock fetching related contacts
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(), // This one matches related contacts query
                default)) // No includes
                .ReturnsAsync(new List<Contact> { relatedContact });

            // Other mocks
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), default)).ReturnsAsync(new List<Note>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(), default)).ReturnsAsync(new List<Reminder>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), default)).ReturnsAsync(new List<SignificantDate>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), default)).ReturnsAsync(new List<Pet>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default)).ReturnsAsync(new List<ContactMethod>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), default)).ReturnsAsync(new List<Fact>());
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default)).ReturnsAsync(new List<Attachment>());


            // Act
            var result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Relationships); // One outgoing
            Assert.Single(result.RelatedTo);     // One incoming

            // Verify manual population of navigation properties
            // The service code populates the Relationship entity's Person/RelatedPerson, and then maps to DTO.
            // DtoMappingExtensions uses:
            // EntityName = entity.Person?.FullName ?? "Unknown",
            // RelatedEntityName = entity.RelatedPerson?.FullName ?? "Unknown",

            var outgoingDto = result.Relationships.First();
            Assert.Equal("Related", outgoingDto.RelatedEntityName.Trim());

            var incomingDto = result.RelatedTo.First();
            Assert.Equal("Related", incomingDto.EntityName.Trim());
        }
    }
}
