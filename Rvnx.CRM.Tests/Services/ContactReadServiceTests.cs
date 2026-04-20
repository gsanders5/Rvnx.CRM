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

namespace Rvnx.CRM.Tests.Services;

public class ContactReadServiceTests
{
    public class ContactReadServiceContactExistsTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceContactExistsTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task ContactExistsAsyncWhenContactExistsAndIsFullReturnsTrue()
        {
            Guid contactId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            bool result = await _service.ContactExistsAsync(contactId);

            Assert.True(result);
        }

        [Fact]
        public async Task ContactExistsAsyncWhenContactDoesNotExistReturnsFalse()
        {
            Guid contactId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            bool result = await _service.ContactExistsAsync(contactId);

            Assert.False(result);
        }

    }

    public class ContactReadServiceGetContactDetailsTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceGetContactDetailsTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task GetContactDetailsAsyncReturnsContactDetailsWithRelationships()
        {
            Guid contactId = Guid.NewGuid();
            Guid relatedId1 = Guid.NewGuid();
            Guid relatedId2 = Guid.NewGuid();

            Contact contact = new()
            { Id = contactId, FirstName = "Main", LastName = "User" };
            List<Contact> relatedContacts =
            [
                new Contact { Id = relatedId1, FirstName = "Child" },
                new Contact { Id = relatedId2, FirstName = "Parent" }
            ];

            List<Relationship> allRelationships =
            [
                new Relationship { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = relatedId1, EntityType = EntityType.Person, RelationshipTypeId = Guid.NewGuid() }, // outgoing
                new Relationship { Id = Guid.NewGuid(), EntityId = relatedId2, RelatedEntityId = contactId, EntityType = EntityType.Person, RelationshipTypeId = Guid.NewGuid() }  // incoming
            ];

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(allRelationships);

            _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Contact>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(relatedContacts);

            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(contactId, result.Id);

            Assert.Single(result.Relationships); // Outgoing
            Assert.Equal(relatedId1, result.Relationships.First().RelatedEntityId);
            Assert.Equal("Child", result.Relationships.First().RelatedEntityName);

            Assert.Single(result.RelatedTo); // Incoming
            Assert.Equal(relatedId2, result.RelatedTo.First().EntityId);
            Assert.Equal("Parent", result.RelatedTo.First().EntityName);
        }

        [Fact]
        public async Task GetContactDetailsAsyncWhenContactDoesNotExistReturnsNull()
        {
            Guid contactId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]); // Returns empty list

            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            Assert.Null(result);
        }
    }

    public class ContactReadServiceGetContactFormTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceGetContactFormTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task GetContactFormAsyncWhenContactDoesNotExistReturnsNull()
        {
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetContactFormAsyncMapsBasicFieldsCorrectly()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Jane",
                LastName = "Doe",
                MaidenName = "Smith",
                Nickname = "Janie",
                JobTitle = "Engineer",
                Company = "Tech Corp",
                IsHidden = true,
                Pronouns = "She/Her",
                Gender = "Female",
                Religion = "Atheist"
            };

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(contact.Id, result.Id);
            Assert.Equal(contact.FirstName, result.FirstName);
            Assert.Equal(contact.LastName, result.LastName);
            Assert.Equal(contact.MaidenName, result.MaidenName);
            Assert.Equal(contact.Nickname, result.Nickname);
            Assert.Equal(contact.JobTitle, result.JobTitle);
            Assert.Equal(contact.Company, result.Company);
            Assert.True(result.IsHidden);
            Assert.Equal(contact.Pronouns, result.Pronouns);
            Assert.Equal(contact.Gender, result.Gender);
            Assert.Equal(contact.Religion, result.Religion);
        }

        [Fact]
        public async Task GetContactFormAsyncPrioritizesPrimaryEmailAndPhone()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "John",
                LastName = "Smith"
            };

            contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Email, Label = "Work", Value = "work@example.com" });
            contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Email, Label = ContactMethodLabels.Primary, Value = "primary@example.com" });

            contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Phone, Label = "Home", Value = "555-1234" });
            contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Phone, Label = ContactMethodLabels.Primary, Value = "555-9999" });

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal("primary@example.com", result.Email);
            Assert.Equal("555-9999", result.Phone);
        }

        [Fact]
        public async Task GetContactFormAsyncMapsBirthdayAndReminderCorrectly()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Birthday",
                LastName = "Person"
            };

            DateOnly birthdayDate = new(1990, 5, 15);
            SignificantDate significantDate = new()
            {
                Title = SignificantDateTitles.Birthday,
                EventDate = birthdayDate
            };
            significantDate.ReminderOffsets.Add(new ReminderOffset { DaysBeforeEvent = 0, IsActive = true });

            contact.SignificantDates.Add(significantDate);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(birthdayDate.ToDateTime(TimeOnly.MinValue), result.Birthday);
            Assert.True(result.RemindOnBirthday);
        }

        [Fact]
        public async Task GetContactFormAsyncMapsBirthdayWithSentinelYearCorrectly()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Birthday",
                LastName = "UnknownYear"
            };

            DateOnly birthdayDate = new(1, 5, 15);
            SignificantDate significantDate = new()
            {
                Title = SignificantDateTitles.Birthday,
                EventDate = birthdayDate
            };

            contact.SignificantDates.Add(significantDate);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(1, result.Birthday?.Year);
            Assert.Equal(5, result.Birthday?.Month);
            Assert.Equal(15, result.Birthday?.Day);
        }

        [Fact]
        public async Task GetContactFormAsyncMapsProfileImageAndLabelsCorrectly()
        {
            Guid contactId = Guid.NewGuid();
            Guid profileImageId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();

            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Image",
                LastName = "Label"
            };
            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId });

            Attachment profileAttachment = new()
            { Id = profileImageId, ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
            List<Label> allLabels =
            [
                new Label { Id = labelId, Name = "Friends", Color = "Red" },
                new Label { Id = Guid.NewGuid(), Name = "Work", Color = "Blue" }
            ];

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListProjectedAsync<Attachment, Guid>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, Guid>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([profileAttachment.Id]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(allLabels);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(profileImageId, result.ProfileImageId);

            Assert.Equal(2, result.AllLabels.Count);
            Assert.Contains(result.AllLabels, l => l.Name == "Friends");
            Assert.Contains(result.AllLabels, l => l.Name == "Work");

            Assert.Single(result.AssignedLabelIds);
            Assert.Equal(labelId, result.AssignedLabelIds.First());
        }
    }

    public class ContactReadServiceIndexTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceIndexTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task GetIndexDataAsyncCorrectlyRestitchesBulkLoadedRelatedEntities()
        {
            Guid contact1Id = Guid.NewGuid();
            Guid contact2Id = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();

            List<ContactDto> contacts =
            [
                new ContactDto { Id = contact1Id, FirstName = "Alice" },
                new ContactDto { Id = contact2Id, FirstName = "Bob" }
            ];

            _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            _repositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact1Id, attachmentId)]);

            _repositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact2Id, labelId, "Friend", "Blue")]);

            _repositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact1Id, new DateOnly(1990, 5, 10))]);

            List<ContactDto> result = await _service.GetIndexDataAsync(false);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            ContactDto alice = result.First(c => c.Id == contact1Id);
            ContactDto bob = result.First(c => c.Id == contact2Id);

            Assert.Equal(attachmentId, alice.ProfileImageId);
            Assert.Empty(alice.Labels);
            Assert.Equal(new DateTime(1990, 5, 10), alice.Birthday);

            Assert.Null(bob.ProfileImageId);
            Assert.Single(bob.Labels);
            Assert.Equal(labelId, bob.Labels.First().Id);
            Assert.Equal("Friend", bob.Labels.First().Name);
            Assert.Equal("Blue", bob.Labels.First().Color);
            Assert.Null(bob.Birthday);
        }

        [Fact]
        public async Task GetIndexDataAsyncGracefullyHandlesMissingOrDuplicateKeys()
        {
            Guid contact1Id = Guid.NewGuid();
            Guid contact2Id = Guid.NewGuid();
            Guid attachment1Id = Guid.NewGuid();
            Guid attachment2Id = Guid.NewGuid();

            List<ContactDto> contacts =
            [
                new ContactDto { Id = contact1Id, FirstName = "Alice" },
                new ContactDto { Id = contact2Id, FirstName = "Bob" } // Bob has no attachments returned
            ];

            _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            // Alice has two profile images (a duplicate condition the system should handle gracefully by picking one)
            // Also returning an attachment for a contact ID that does NOT exist in our DTO list (e.g., an orphaned or mistmatched record)
            Guid missingContactId = Guid.NewGuid();
            _repositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    (contact1Id, attachment1Id),
                    (contact1Id, attachment2Id), // Duplicate
                    (missingContactId, Guid.NewGuid()) // Key not in map
                ]);

            _repositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            List<ContactDto> result = await _service.GetIndexDataAsync(false);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            ContactDto alice = result.First(c => c.Id == contact1Id);
            ContactDto bob = result.First(c => c.Id == contact2Id);

            // Alice should have one of the profile images (it uses GroupBy.First(), so it should be attachment1Id)
            Assert.Equal(attachment1Id, alice.ProfileImageId);

            // Bob should have null ProfileImageId, since he had no attachments in the db query
            Assert.Null(bob.ProfileImageId);

            // Ensure no exception was thrown by the missingContactId
        }
    }

    public class ContactReadServiceLabelOptimizationTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceLabelOptimizationTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task GetContactFormAsyncFetchesLabelsEagerly()
        {
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId });

            // We use It.IsAny<string[]> because we are testing if the optimization works regardless of exact includes for now,
            // but we expect "ContactLabels" to be present eventually.
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
               It.IsAny<Expression<Func<Label, bool>>>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<string[]>()))
               .ReturnsAsync([]);

            // If the code uses this query, result.AssignedLabelIds will be EMPTY.
            // If the code uses contact.ContactLabels, result.AssignedLabelIds will contain labelId.
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);

            Assert.Contains(labelId, result.AssignedLabelIds);

            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GetContactDetailsAsyncFetchesLabelsEagerly()
        {
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Label label = new() { Id = labelId, Name = "Test Label", Color = "Blue" };

            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId, Label = label });

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Setup related entities (needed for GetContactDetailsAsync) to return empty lists to avoid null ref if logic expects them
            // We can rely on default null/empty behavior if logic handles it, but let's be safe
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default)).ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);

            Assert.Single(result.Labels);
            Assert.Equal("Test Label", result.Labels.First().Name);

            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Never);
        }
    }

    public class ContactReadServiceGetContactNamesTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFavoriteService> _favoriteServiceMock;
        private readonly ContactReadService _service;

        public ContactReadServiceGetContactNamesTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _favoriteServiceMock = new Mock<IFavoriteService>();
            _favoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);
            _service = new ContactReadService(_repositoryMock.Object, _favoriteServiceMock.Object);
        }

        [Fact]
        public async Task GetContactNamesAsyncEvaluatesProjectionCorrectly()
        {
            // Arrange
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            List<Contact> testContacts =
            [
                new Contact { Id = id1, FirstName = "John", LastName = "Doe", IsPartial = false, IsHidden = false },
                new Contact { Id = id2, FirstName = "Jane", LastName = "Smith", IsPartial = true, IsHidden = false }
            ];

            Expression<Func<Contact, (Guid, string)>>? capturedProjection = null;
            Expression<Func<Contact, bool>>? capturedFilter = null;

            _repositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (filter, projection, ct) =>
                    {
                        capturedFilter = filter;
                        capturedProjection = projection;
                    })
                .ReturnsAsync(new List<(Guid, string)>()); // Return value doesn't matter, we evaluate the expression

            // Act
            await _service.GetContactNamesAsync();

            // Assert
            Assert.NotNull(capturedProjection);
            Assert.NotNull(capturedFilter);

            var filterFunc = capturedFilter.Compile();
            var projectionFunc = capturedProjection.Compile();

            // Validate the filter works
            Assert.True(filterFunc(testContacts[0]));
            Assert.True(filterFunc(testContacts[1]));
            Assert.False(filterFunc(new Contact { IsHidden = true }));

            // Validate the projection logic on real in-memory objects
            var projectedFull = projectionFunc(testContacts[0]);
            Assert.Equal(id1, projectedFull.Item1);
            Assert.Equal("John Doe", projectedFull.Item2);

            var projectedPartial = projectionFunc(testContacts[1]);
            Assert.Equal(id2, projectedPartial.Item1);
            Assert.Equal("Jane Smith (partial contact)", projectedPartial.Item2);
        }

        [Fact]
        public async Task HasRelationshipsAsyncEvaluatesFilterCorrectly()
        {
            // Arrange
            Guid queryId = Guid.NewGuid();
            Guid otherId = Guid.NewGuid();

            Expression<Func<Relationship, bool>>? capturedFilter = null;

            _repositoryMock.Setup(r => r.CountAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Relationship, bool>>, CancellationToken>(
                    (filter, ct) => capturedFilter = filter)
                .ReturnsAsync(0);

            // Act
            await _service.HasRelationshipsAsync(queryId);

            // Assert
            Assert.NotNull(capturedFilter);
            var filterFunc = capturedFilter.Compile();

            // Should match: entity is queryId and type is person
            Assert.True(filterFunc(new Relationship { EntityId = queryId, RelatedEntityId = otherId, EntityType = EntityType.Person }));
            // Should match: related entity is queryId and type is person
            Assert.True(filterFunc(new Relationship { EntityId = otherId, RelatedEntityId = queryId, EntityType = EntityType.Person }));

            // Should not match: neither ID matches
            Assert.False(filterFunc(new Relationship { EntityId = Guid.NewGuid(), RelatedEntityId = otherId, EntityType = EntityType.Person }));
            // Should not match: wrong entity type
            Assert.False(filterFunc(new Relationship { EntityId = queryId, RelatedEntityId = otherId, EntityType = EntityType.Company }));
        }
    }
}