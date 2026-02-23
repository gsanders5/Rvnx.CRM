using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
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
        public async Task GetContactDetailsAsyncReturnsNullWhenContactNotFound()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetContactDetailsAsyncReturnsDtoWhenContactFound()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Setup related entities to return empty lists to avoid null reference if logic assumes lists
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), default))
                .ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(contactId, result.Id);
            Assert.Equal("Test", result.FirstName);
        }

        [Fact]
        public async Task GetContactDetailsAsyncPopulatesRelationshipsCorrectly()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid relatedId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Main" };
            Contact relatedContact = new() { Id = relatedId, FirstName = "Related" };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                 It.Is<Expression<Func<Contact, bool>>>(expr => expr.Compile().Invoke(contact)), // Matches ID check
                 It.IsAny<CancellationToken>(),
                 It.IsAny<string[]>()))
                 .ReturnsAsync([contact]);

            // Mock Relationships query
            // Using known IDs from RelationshipTypeService
            // Friend: a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d
            // Colleague: 33333333-3333-3333-3333-333333333301
            List<Relationship> relationships =
            [
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
            ];

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                default))
                .ReturnsAsync(relationships);

            // Mock fetching related contacts
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(), // This one matches related contacts query
                default)) // No includes
                .ReturnsAsync([relatedContact]);

            // Other mocks
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Reminder>(It.IsAny<Expression<Func<Reminder, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default)).ReturnsAsync([]);


            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Relationships); // One outgoing
            Assert.Single(result.RelatedTo);     // One incoming

            // Verify manual population of navigation properties
            // The service code populates the Relationship entity's Person/RelatedPerson, and then maps to DTO.
            // DtoMappingExtensions uses:
            // EntityName = entity.Person?.FullName ?? "Unknown",
            // RelatedEntityName = entity.RelatedPerson?.FullName ?? "Unknown",

            RelationshipDto outgoingDto = result.Relationships.First();
            Assert.Equal("Related", outgoingDto.RelatedEntityName.Trim());

            RelationshipDto incomingDto = result.RelatedTo.First();
            Assert.Equal("Related", incomingDto.EntityName.Trim());
        }

        [Fact]
        public async Task GetIndexDataAsyncFiltersHiddenContactsCorrectly()
        {
            // Arrange
            Contact hiddenContact = new() { Id = Guid.NewGuid(), FirstName = "Hidden", IsHidden = true };
            Contact visibleContact = new() { Id = Guid.NewGuid(), FirstName = "Visible", IsHidden = false };

            // We mock the repository to filter based on the predicate passed to it
            // This is crucial to verify that the service is constructing the query correctly
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                default,
                It.IsAny<string[]>())) // No includes
                .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    List<Contact> all = [hiddenContact, visibleContact];
                    return all.AsQueryable().Where(predicate).ToList();
                });

            // Mock subsequent calls to return empty
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(It.IsAny<Expression<Func<ContactLabel, bool>>>(), default, It.IsAny<string[]>())).ReturnsAsync([]);

            // Act - Show Hidden
            List<ContactDto> resultShowHidden = await _service.GetIndexDataAsync(true);

            // Assert
            Assert.Single(resultShowHidden);
            Assert.Equal("Hidden", resultShowHidden.First().FirstName);

            // Act - Hide Hidden (Show Visible only)
            List<ContactDto> resultHideHidden = await _service.GetIndexDataAsync(false);

            // Assert
            Assert.Single(resultHideHidden);
            Assert.Equal("Visible", resultHideHidden.First().FirstName);
        }

        [Fact]
        public async Task GetIndexDataAsyncMapsProfileImagesCorrectly()
        {
            // Arrange
            Guid contactId1 = Guid.NewGuid();
            Guid contactId2 = Guid.NewGuid();
            Guid imageId1 = Guid.NewGuid();

            Contact contact1 = new() { Id = contactId1, FirstName = "C1" };
            Contact contact2 = new() { Id = contactId2, FirstName = "C2" };

            Attachment image1 = new()
            {
                Id = imageId1,
                ContactId = contactId1,
                AttachmentType = AttachmentTypes.ProfileImage
            };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([contact1, contact2]);

            // Mock profile image fetch
            // The service fetches by AttachmentType == ProfileImage AND ContactId in list
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                default))
                .ReturnsAsync([image1]);

             _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(It.IsAny<Expression<Func<ContactLabel, bool>>>(), default, It.IsAny<string[]>())).ReturnsAsync([]);

            // Act
            List<ContactDto> result = await _service.GetIndexDataAsync(false);

            // Assert
            Assert.Equal(2, result.Count);

            ContactDto? dto1 = result.FirstOrDefault(c => c.Id == contactId1);
            Assert.NotNull(dto1);
            Assert.Equal(imageId1, dto1.ProfileImageId);

            ContactDto? dto2 = result.FirstOrDefault(c => c.Id == contactId2);
            Assert.NotNull(dto2);
            Assert.Null(dto2.ProfileImageId);
        }

        [Fact]
        public async Task GetIndexDataAsyncMapsLabelsCorrectly()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "C1" };

            Label label1 = new() { Id = Guid.NewGuid(), Name = "VIP", Color = "Red" };
            ContactLabel cl1 = new() { ContactId = contactId, LabelId = label1.Id, Label = label1 };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), default)).ReturnsAsync([]);

            // Mock labels fetch
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                default,
                It.IsAny<string[]>()))
                .ReturnsAsync([cl1]);

            // Act
            List<ContactDto> result = await _service.GetIndexDataAsync(false);

            // Assert
            Assert.Single(result);
            ContactDto dto = result.First();
            Assert.Single(dto.Labels);
            Assert.Equal("VIP", dto.Labels.First().Name);
            Assert.Equal("Red", dto.Labels.First().Color);
        }

        [Fact]
        public async Task GetContactFormAsyncSelectsPrimaryEmail()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test" };

            ContactMethod primaryEmail = new() { Type = ContactMethodType.Email, Value = "primary@example.com", Label = ContactMethodLabels.Primary };
            ContactMethod secondaryEmail = new() { Type = ContactMethodType.Email, Value = "secondary@example.com", Label = "Work" };

            contact.ContactMethods.Add(secondaryEmail);
            contact.ContactMethods.Add(primaryEmail); // Order shouldn't matter for logic, but let's mix it up

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(It.IsAny<Expression<Func<ContactLabel, bool>>>(), default, It.IsAny<string[]>())).ReturnsAsync([]);

            // Act
            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("primary@example.com", result.Email);
        }

        [Fact]
        public async Task GetContactFormAsyncFallbacksToAnyEmail()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test" };

            ContactMethod anyEmail = new() { Type = ContactMethodType.Email, Value = "any@example.com", Label = "Work" };

            contact.ContactMethods.Add(anyEmail);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), default)).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(It.IsAny<Expression<Func<ContactLabel, bool>>>(), default, It.IsAny<string[]>())).ReturnsAsync([]);

            // Act
            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("any@example.com", result.Email);
        }

        [Fact]
        public async Task GetContactFormAsyncPopulatesLabels()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test" };

            Label label1 = new() { Id = Guid.NewGuid(), Name = "A-Label" };
            Label label2 = new() { Id = Guid.NewGuid(), Name = "B-Label" };

            ContactLabel assignedLabel = new() { ContactId = contactId, LabelId = label1.Id };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), default))
                .ReturnsAsync([label2, label1]); // Unsorted

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(It.IsAny<Expression<Func<ContactLabel, bool>>>(), default, It.IsAny<string[]>()))
                .ReturnsAsync([assignedLabel]);

            // Act
            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            // Assert
            Assert.NotNull(result);

            // Check AllLabels are sorted
            Assert.Equal(2, result.AllLabels.Count);
            Assert.Equal("A-Label", result.AllLabels[0].Name);
            Assert.Equal("B-Label", result.AllLabels[1].Name);

            // Check AssignedLabelIds
            Assert.Single(result.AssignedLabelIds);
            Assert.Contains(label1.Id, result.AssignedLabelIds);
        }
    }
}
