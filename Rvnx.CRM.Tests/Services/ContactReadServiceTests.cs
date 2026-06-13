using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Activity;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactReadServiceTests
{
    /// <summary>
    /// Shared scaffolding for nested test classes — wires up a <see cref="ContactReadService"/>
    /// backed by Moq'd dependencies with sensible defaults.
    /// </summary>
    public abstract class ContactReadServiceTestBase
    {
        protected Mock<IRepository> RepositoryMock { get; }
        protected Mock<IFavoriteService> FavoriteServiceMock { get; }
        protected ContactReadService Service { get; }

        protected ContactReadServiceTestBase()
        {
            RepositoryMock = new Mock<IRepository>();
            FavoriteServiceMock = new Mock<IFavoriteService>();
            FavoriteServiceMock.Setup(f => f.GetFavoriteContactIdsAsync()).ReturnsAsync([]);

            // GetIndexDataAsync always queries activity dates for the Last Contact column;
            // default to empty so tests that don't care about activities need no setup.
            RepositoryMock.Setup(r => r.ListProjectedAsync<ActivityContact, (Guid, DateTime)>(
                It.IsAny<Expression<Func<ActivityContact, bool>>>(),
                It.IsAny<Expression<Func<ActivityContact, (Guid, DateTime)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            Service = new ContactReadService(RepositoryMock.Object, FavoriteServiceMock.Object);
        }
    }

    public class ContactReadServiceContactExistsTests : ContactReadServiceTestBase
    {

        [Fact]
        public async Task ContactExistsAsyncWhenContactExistsAndIsFullReturnsTrue()
        {
            Guid contactId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            bool result = await Service.ContactExistsAsync(contactId);

            Assert.True(result);
        }

        [Fact]
        public async Task ContactExistsAsyncWhenContactDoesNotExistReturnsFalse()
        {
            Guid contactId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            bool result = await Service.ContactExistsAsync(contactId);

            Assert.False(result);
        }

    }

    public class ContactReadServiceGetContactDetailsTests : ContactReadServiceTestBase
    {

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
                new Relationship { Id = Guid.NewGuid(), ContactId = contactId, RelatedContactId = relatedId1, RelationshipTypeId = Guid.NewGuid() }, // outgoing
                new Relationship { Id = Guid.NewGuid(), ContactId = relatedId2, RelatedContactId = contactId, RelationshipTypeId = Guid.NewGuid() }  // incoming
            ];

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(allRelationships);

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Contact>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(relatedContacts);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(contactId, result.Id);

            Assert.Single(result.Relationships); // Outgoing
            Assert.Equal(relatedId1, result.Relationships.First().RelatedContactId);
            Assert.Equal("Child", result.Relationships.First().RelatedContactName);

            Assert.Single(result.RelatedTo); // Incoming
            Assert.Equal(relatedId2, result.RelatedTo.First().ContactId);
            Assert.Equal("Parent", result.RelatedTo.First().ContactName);
        }

        [Fact]
        public async Task GetContactDetailsAsyncWhenContactDoesNotExistReturnsNull()
        {
            Guid contactId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]); // Returns empty list

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetContactDetailsAsyncReturnsNullForPartialContact()
        {
            Guid contactId = Guid.NewGuid();
            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .Callback<Expression<Func<Contact, bool>>, CancellationToken, string[]>(
                    (filter, ct, includes) => capturedFilter = filter)
                .ReturnsAsync([]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.Null(result);
            Assert.NotNull(capturedFilter);

            Func<Contact, bool> filterFunc = capturedFilter.Compile();
            Assert.False(filterFunc(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true }));
            Assert.True(filterFunc(new Contact { Id = contactId, FirstName = "Full", IsPartial = false }));
        }

        [Fact]
        public async Task GetContactDetailsAsyncProjectsIsDeceasedOnRelatedContacts()
        {
            // The Relationships panel needs IsDeceased on each related contact so it can render the
            // deceased badge next to the name without re-querying.
            Guid contactId = Guid.NewGuid();
            Guid livingId = Guid.NewGuid();
            Guid deceasedId = Guid.NewGuid();

            Contact living = new() { Id = livingId, FirstName = "Alive", IsDeceased = false };
            Contact deceased = new() { Id = deceasedId, FirstName = "Late", IsDeceased = true };

            Expression<Func<Contact, Contact>>? capturedProjection = null;

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([new Contact { Id = contactId, FirstName = "Main" }]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([
                    new Relationship { Id = Guid.NewGuid(), ContactId = contactId, RelatedContactId = livingId, RelationshipTypeId = Guid.NewGuid() },
                    new Relationship { Id = Guid.NewGuid(), ContactId = contactId, RelatedContactId = deceasedId, RelationshipTypeId = Guid.NewGuid() }
                ]);

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Contact>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, Contact>>, CancellationToken>(
                    (_, projection, _) => capturedProjection = projection)
                .ReturnsAsync([living, deceased]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            // Confirm the projection itself carries IsDeceased through.
            Assert.NotNull(capturedProjection);
            Func<Contact, Contact> projectionFunc = capturedProjection.Compile();
            Assert.False(projectionFunc(living).IsDeceased);
            Assert.True(projectionFunc(deceased).IsDeceased);

            // And confirm the flag rides into the per-relationship DTO surface used by the view.
            Assert.NotNull(result);
            Assert.Equal(2, result.Relationships.Count());
            RelationshipDto livingRel = result.Relationships.Single(r => r.RelatedContactId == livingId);
            RelationshipDto deceasedRel = result.Relationships.Single(r => r.RelatedContactId == deceasedId);
            Assert.False(livingRel.IsRelatedContactDeceased);
            Assert.True(deceasedRel.IsRelatedContactDeceased);
        }

        [Fact]
        public async Task GetContactDetailsAsyncHandlesContactWithNoRelationships()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Solo", LastName = "User" };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Empty(result.Relationships);
            Assert.Empty(result.RelatedTo);
        }

        [Fact]
        public async Task GetContactDetailsAsyncProjectsIsDeceasedOnActivityParticipants()
        {
            // The Activities card lists multi-contact attendees; each participant needs a deceased flag
            // so the view can render the bi-flower1 indicator inline without re-querying.
            Guid contactId = Guid.NewGuid();
            Guid activityId = Guid.NewGuid();
            Guid livingId = Guid.NewGuid();
            Guid deceasedId = Guid.NewGuid();

            Contact self = new()
            {
                Id = contactId,
                FirstName = "Main",
                LastName = "User"
            };
            Activity activity = new()
            {
                Id = activityId,
                Title = "Dinner",
                ActivityDate = DateTime.UtcNow
            };
            self.ActivityContacts.Add(new ActivityContact { ActivityId = activityId, ContactId = contactId, Activity = activity });

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([self]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Participant rows: self + two other attendees.
            RepositoryMock.Setup(r => r.ListProjectedAsync<ActivityContact, (Guid, Guid)>(
                It.IsAny<Expression<Func<ActivityContact, bool>>>(),
                It.IsAny<Expression<Func<ActivityContact, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    (activityId, contactId),
                    (activityId, livingId),
                    (activityId, deceasedId)
                ]);

            // The participant info projection now carries (Id, Name, IsDeceased).
            Expression<Func<Contact, (Guid, string, bool)>>? capturedProjection = null;
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, (Guid, string, bool)>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string, bool)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string, bool)>>, CancellationToken>(
                    (_, projection, _) => capturedProjection = projection)
                .ReturnsAsync([
                    (livingId, "Liv Ing", false),
                    (deceasedId, "Late Friend", true)
                ]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            // The projection itself must lift IsDeceased from the entity.
            Assert.NotNull(capturedProjection);
            Func<Contact, (Guid, string, bool)> projectionFunc = capturedProjection.Compile();
            Assert.False(projectionFunc(new Contact { Id = livingId, FirstName = "Liv", IsDeceased = false }).Item3);
            Assert.True(projectionFunc(new Contact { Id = deceasedId, FirstName = "Late", IsDeceased = true }).Item3);

            // And the per-participant deceased flag rides into the ActivityDto, parallel to ContactNames/ContactIds.
            Assert.NotNull(result);
            ActivityDto activityDto = Assert.Single(result.Activities);
            Assert.Equal(2, activityDto.ContactIds.Count);
            Assert.Equal(activityDto.ContactIds.Count, activityDto.ContactIsDeceased.Count);

            int livingIdx = activityDto.ContactIds.IndexOf(livingId);
            int deceasedIdx = activityDto.ContactIds.IndexOf(deceasedId);
            Assert.False(activityDto.ContactIsDeceased[livingIdx]);
            Assert.True(activityDto.ContactIsDeceased[deceasedIdx]);
        }

        [Fact]
        public async Task GetContactDetailsAsyncProjectsIsDeceasedOnPetCoOwners()
        {
            // The Pets card lists co-owners; each owner needs a deceased flag so the view can render
            // the bi-flower1 indicator next to a deceased co-owner without re-querying.
            Guid contactId = Guid.NewGuid();
            Guid petId = Guid.NewGuid();
            Guid livingOwnerId = Guid.NewGuid();
            Guid deceasedOwnerId = Guid.NewGuid();

            Contact self = new()
            {
                Id = contactId,
                FirstName = "Main",
                LastName = "User"
            };
            Pet pet = new()
            {
                Id = petId,
                Name = "Rex",
                Species = "Dog"
            };
            self.PetContacts.Add(new PetContact { PetId = petId, ContactId = contactId, Pet = pet });

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([self]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // PetContact rows: self + two co-owners.
            RepositoryMock.Setup(r => r.ListProjectedAsync<PetContact, (Guid, Guid)>(
                It.IsAny<Expression<Func<PetContact, bool>>>(),
                It.IsAny<Expression<Func<PetContact, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    (petId, contactId),
                    (petId, livingOwnerId),
                    (petId, deceasedOwnerId)
                ]);

            Expression<Func<Contact, (Guid, string, bool)>>? capturedProjection = null;
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, (Guid, string, bool)>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string, bool)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string, bool)>>, CancellationToken>(
                    (_, projection, _) => capturedProjection = projection)
                .ReturnsAsync([
                    (livingOwnerId, "Co Owner", false),
                    (deceasedOwnerId, "Late Owner", true)
                ]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(capturedProjection);
            Func<Contact, (Guid, string, bool)> projectionFunc = capturedProjection.Compile();
            Assert.False(projectionFunc(new Contact { Id = livingOwnerId, FirstName = "Co", IsDeceased = false }).Item3);
            Assert.True(projectionFunc(new Contact { Id = deceasedOwnerId, FirstName = "Late", IsDeceased = true }).Item3);

            Assert.NotNull(result);
            PetDto petDto = Assert.Single(result.Pets);

            // Self should be excluded; only the two co-owners remain — one each living/deceased.
            Assert.Equal(2, petDto.Owners.Count);
            Assert.DoesNotContain(petDto.Owners, o => o.Id == contactId);

            PetOwnerDto livingOwner = petDto.Owners.Single(o => o.Id == livingOwnerId);
            PetOwnerDto deceasedOwner = petDto.Owners.Single(o => o.Id == deceasedOwnerId);
            Assert.False(livingOwner.IsDeceased);
            Assert.True(deceasedOwner.IsDeceased);
        }
    }

    public class ContactReadServiceGetContactFormTests : ContactReadServiceTestBase
    {

        [Fact]
        public async Task GetContactFormAsyncWhenContactDoesNotExistReturnsNull()
        {
            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(Guid.NewGuid());

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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListProjectedAsync<Attachment, Guid>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, Guid>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([profileAttachment.Id]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync(allLabels);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(profileImageId, result.ProfileImageId);

            Assert.Equal(2, result.AllLabels.Count);
            Assert.Contains(result.AllLabels, l => l.Name == "Friends");
            Assert.Contains(result.AllLabels, l => l.Name == "Work");

            Assert.Single(result.AssignedLabelIds);
            Assert.Equal(labelId, result.AssignedLabelIds.First());
        }
    }

    public class ContactReadServiceIndexTests : ContactReadServiceTestBase
    {

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

            RepositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            RepositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact1Id, attachmentId)]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact2Id, labelId, "Friend", "Blue")]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([(contact1Id, new DateOnly(1990, 5, 10))]);

            List<ContactDto> result = await Service.GetIndexDataAsync(false);

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
        public async Task GetIndexDataAsyncWhenShowHiddenIncludesDeceased()
        {
            // The "View Hidden / Deceased" toggle surfaces hidden and deceased contacts alongside everyone else.
            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactDto>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            await Service.GetIndexDataAsync(showHidden: true);

            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filter = capturedFilter.Compile();

            // Visible: living, hidden, deceased — anything not partial.
            Assert.True(filter(new Contact { FirstName = "Alive", IsHidden = false, IsDeceased = false, IsPartial = false }));
            Assert.True(filter(new Contact { FirstName = "Hidden", IsHidden = true, IsDeceased = false, IsPartial = false }));
            Assert.True(filter(new Contact { FirstName = "Late", IsHidden = false, IsDeceased = true, IsPartial = false }));
            Assert.True(filter(new Contact { FirstName = "HiddenAndLate", IsHidden = true, IsDeceased = true, IsPartial = false }));

            // Partial contacts are still excluded from the index regardless of the toggle.
            Assert.False(filter(new Contact { FirstName = "Partial", IsPartial = true }));
        }

        [Fact]
        public async Task GetIndexDataAsyncExcludesDeceasedByDefault()
        {
            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactDto>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            await Service.GetIndexDataAsync(showHidden: false);

            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filter = capturedFilter.Compile();

            Assert.True(filter(new Contact { FirstName = "Alive", IsHidden = false, IsDeceased = false, IsPartial = false }));
            Assert.False(filter(new Contact { FirstName = "Hidden", IsHidden = true, IsDeceased = false, IsPartial = false }));
            Assert.False(filter(new Contact { FirstName = "Late", IsHidden = false, IsDeceased = true, IsPartial = false }));
            Assert.False(filter(new Contact { FirstName = "HiddenAndLate", IsHidden = true, IsDeceased = true, IsPartial = false }));
            Assert.False(filter(new Contact { FirstName = "Partial", IsPartial = true }));
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

            RepositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(contacts);

            // Alice has two profile images (a duplicate condition the system should handle gracefully by picking one)
            // Also returning an attachment for a contact ID that does NOT exist in our DTO list (e.g., an orphaned or mistmatched record)
            Guid missingContactId = Guid.NewGuid();
            RepositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    (contact1Id, attachment1Id),
                    (contact1Id, attachment2Id), // Duplicate
                    (missingContactId, Guid.NewGuid()) // Key not in map
                ]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            List<ContactDto> result = await Service.GetIndexDataAsync(false);

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

        [Fact]
        public async Task GetIndexDataAsyncProjectsIsDeceasedAndDateOfDeath()
        {
            Guid livingId = Guid.NewGuid();
            Guid deceasedId = Guid.NewGuid();
            DateOnly dateOfDeath = new(2024, 5, 1);

            Contact living = new() { Id = livingId, FirstName = "Alive", LastName = "One", IsHidden = false };
            Contact deceased = new() { Id = deceasedId, FirstName = "Late", LastName = "Two", IsHidden = false, IsDeceased = true, DateOfDeath = dateOfDeath };

            Expression<Func<Contact, ContactDto>>? capturedProjection = null;

            RepositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactDto>>, CancellationToken>(
                    (_, projection, _) => capturedProjection = projection)
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            await Service.GetIndexDataAsync(false);

            Assert.NotNull(capturedProjection);
            Func<Contact, ContactDto> projectionFunc = capturedProjection.Compile();

            ContactDto livingDto = projectionFunc(living);
            Assert.False(livingDto.IsDeceased);
            Assert.Null(livingDto.DateOfDeath);

            ContactDto deceasedDto = projectionFunc(deceased);
            Assert.True(deceasedDto.IsDeceased);
            Assert.Equal(dateOfDeath, deceasedDto.DateOfDeath);
        }
    }

    public class ContactReadServiceLabelOptimizationTests : ContactReadServiceTestBase
    {

        [Fact]
        public async Task GetContactFormAsyncFetchesLabelsEagerly()
        {
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId });

            // We use It.IsAny<string[]> because we are testing if the optimization works regardless of exact includes for now,
            // but we expect "ContactLabels" to be present eventually.
            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
               It.IsAny<Expression<Func<Label, bool>>>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<string[]>()))
               .ReturnsAsync([]);

            // If the code uses this query, result.AssignedLabelIds will be EMPTY.
            // If the code uses contact.ContactLabels, result.AssignedLabelIds will contain labelId.
            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

            Assert.NotNull(result);

            Assert.Contains(labelId, result.AssignedLabelIds);

            RepositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
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

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Setup related entities (needed for GetContactDetailsAsync) to return empty lists to avoid null ref if logic expects them
            // We can rely on default null/empty behavior if logic handles it, but let's be safe
            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default)).ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);

            Assert.Single(result.Labels);
            Assert.Equal("Test Label", result.Labels.First().Name);

            RepositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Never);
        }
    }

    public class ContactReadServiceGetContactNamesTests : ContactReadServiceTestBase
    {

        [Fact]
        public async Task GetContactNamesAsyncEvaluatesProjectionCorrectly()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            List<Contact> testContacts =
            [
                new Contact { Id = id1, FirstName = "John", LastName = "Doe", IsPartial = false, IsHidden = false },
                new Contact { Id = id2, FirstName = "Jane", LastName = "Smith", IsPartial = true, IsHidden = false }
            ];

            Expression<Func<Contact, (Guid, string)>>? capturedProjection = null;
            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (filter, projection, ct) =>
                    {
                        capturedFilter = filter;
                        capturedProjection = projection;
                    })
                .ReturnsAsync([]); // Return value doesn't matter, we evaluate the expression

            await Service.GetContactNamesAsync();

            Assert.NotNull(capturedProjection);
            Assert.NotNull(capturedFilter);

            Func<Contact, bool> filterFunc = capturedFilter.Compile();
            Func<Contact, (Guid, string)> projectionFunc = capturedProjection.Compile();

            Assert.True(filterFunc(testContacts[0]));
            Assert.True(filterFunc(testContacts[1]));
            Assert.False(filterFunc(new Contact { IsHidden = true }));

            (Guid, string) projectedFull = projectionFunc(testContacts[0]);
            Assert.Equal(id1, projectedFull.Item1);
            Assert.Equal("John Doe", projectedFull.Item2);

            (Guid, string) projectedPartial = projectionFunc(testContacts[1]);
            Assert.Equal(id2, projectedPartial.Item1);
            Assert.Equal("Jane Smith (Partial Contact)", projectedPartial.Item2);
        }

        [Fact]
        public async Task GetContactNamesAsyncProjectionAppendsDeceasedSuffix()
        {
            Guid deceasedId = Guid.NewGuid();
            Guid partialDeceasedId = Guid.NewGuid();

            List<Contact> testContacts =
            [
                new Contact { Id = deceasedId, FirstName = "Late", LastName = "Person", IsPartial = false, IsDeceased = true, IsHidden = false },
                new Contact { Id = partialDeceasedId, FirstName = "Ghost", LastName = "Soul", IsPartial = true, IsDeceased = true, IsHidden = false }
            ];

            Expression<Func<Contact, (Guid, string)>>? capturedProjection = null;

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (_, projection, _) => capturedProjection = projection)
                .ReturnsAsync([]);

            await Service.GetContactNamesAsync();

            Assert.NotNull(capturedProjection);
            Func<Contact, (Guid, string)> projectionFunc = capturedProjection.Compile();

            (Guid, string) projectedDeceased = projectionFunc(testContacts[0]);
            Assert.Equal(deceasedId, projectedDeceased.Item1);
            Assert.Equal("Late Person (Deceased)", projectedDeceased.Item2);

            (Guid, string) projectedPartialDeceased = projectionFunc(testContacts[1]);
            Assert.Equal(partialDeceasedId, projectedPartialDeceased.Item1);
            Assert.Equal("Ghost Soul (Partial Contact, Deceased)", projectedPartialDeceased.Item2);
        }

        [Fact]
        public async Task GetContactNamesAsyncWhenExcludeDeceasedFiltersDeceasedContacts()
        {
            Guid livingId = Guid.NewGuid();
            Guid deceasedId = Guid.NewGuid();

            Contact living = new() { Id = livingId, FirstName = "Alive", LastName = "Person", IsHidden = false, IsDeceased = false };
            Contact deceased = new() { Id = deceasedId, FirstName = "Late", LastName = "Person", IsHidden = false, IsDeceased = true };
            Contact hidden = new() { Id = Guid.NewGuid(), FirstName = "Hidden", IsHidden = true };

            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            await Service.GetContactNamesAsync(excludeDeceased: true);

            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filterFunc = capturedFilter.Compile();

            Assert.True(filterFunc(living));
            Assert.False(filterFunc(deceased));
            Assert.False(filterFunc(hidden));
        }

        [Fact]
        public async Task GetContactNamesAsyncWhenExcludeDeceasedKeepsAlwaysIncludeIds()
        {
            // Arrange — used on edit forms so an already-attached deceased participant remains selectable.
            Guid livingId = Guid.NewGuid();
            Guid keepDeceasedId = Guid.NewGuid();
            Guid otherDeceasedId = Guid.NewGuid();

            Contact living = new() { Id = livingId, FirstName = "Alive", IsHidden = false, IsDeceased = false };
            Contact keepDeceased = new() { Id = keepDeceasedId, FirstName = "Already", LastName = "Attached", IsHidden = false, IsDeceased = true };
            Contact otherDeceased = new() { Id = otherDeceasedId, FirstName = "Different", IsHidden = false, IsDeceased = true };

            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            await Service.GetContactNamesAsync(excludeDeceased: true, alwaysIncludeIds: [keepDeceasedId]);

            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filterFunc = capturedFilter.Compile();

            Assert.True(filterFunc(living));
            Assert.True(filterFunc(keepDeceased));   // already attached — kept visible
            Assert.False(filterFunc(otherDeceased)); // unrelated deceased — filtered out
        }

        [Fact]
        public async Task GetContactNamesAsyncDefaultsToIncludingDeceasedContacts()
        {
            // Arrange — historical / structural surfaces (merge, relationships) keep deceased visible.
            Guid deceasedId = Guid.NewGuid();
            Contact deceased = new() { Id = deceasedId, FirstName = "Late", IsHidden = false, IsDeceased = true };

            Expression<Func<Contact, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, (Guid, string)>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, (Guid, string)>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            await Service.GetContactNamesAsync();

            Assert.NotNull(capturedFilter);
            Assert.True(capturedFilter.Compile()(deceased));
        }

        [Fact]
        public async Task HasRelationshipsAsyncEvaluatesFilterCorrectly()
        {
            Guid queryId = Guid.NewGuid();
            Guid otherId = Guid.NewGuid();

            Expression<Func<Relationship, bool>>? capturedFilter = null;

            RepositoryMock.Setup(r => r.CountAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Relationship, bool>>, CancellationToken>(
                    (filter, ct) => capturedFilter = filter)
                .ReturnsAsync(0);

            await Service.HasRelationshipsAsync(queryId);

            Assert.NotNull(capturedFilter);
            Func<Relationship, bool> filterFunc = capturedFilter.Compile();

            // Should match: contact is queryId
            Assert.True(filterFunc(new Relationship { ContactId = queryId, RelatedContactId = otherId }));
            // Should match: related contact is queryId
            Assert.True(filterFunc(new Relationship { ContactId = otherId, RelatedContactId = queryId }));

            // Should not match: neither ID matches
            Assert.False(filterFunc(new Relationship { ContactId = Guid.NewGuid(), RelatedContactId = otherId }));
        }
    }

    public class ContactReadServiceGetIntroducerCandidatesTests : ContactReadServiceTestBase
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetIntroducerCandidatesAsync_WhenCalled_ReturnsCandidatesAlphabetically()
        {
            // Arrange
            List<ContactSelectItemDto> mockData =
            [
                new() { Id = Guid.NewGuid(), FullName = "Zebra" },
                new() { Id = Guid.NewGuid(), FullName = "Apple" },
                new() { Id = Guid.NewGuid(), FullName = "Mango" }
            ];

            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockData);

            // Act
            List<ContactSelectItemDto> result = await Service.GetIntroducerCandidatesAsync(null);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Apple", result[0].FullName);
            Assert.Equal("Mango", result[1].FullName);
            Assert.Equal("Zebra", result[2].FullName);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetIntroducerCandidatesAsync_WhenCalled_ExcludesPartialContacts()
        {
            // Arrange
            Expression<Func<Contact, bool>>? capturedFilter = null;
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactSelectItemDto>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            // Act
            await Service.GetIntroducerCandidatesAsync(null);

            // Assert
            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filterFunc = capturedFilter.Compile();
            Assert.False(filterFunc(new Contact { IsPartial = true }));
            Assert.True(filterFunc(new Contact { IsPartial = false }));
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetIntroducerCandidatesAsync_WhenExcludeContactIdIsProvided_ExcludesSpecificContact()
        {
            // Arrange
            Guid excludeId = Guid.NewGuid();
            Expression<Func<Contact, bool>>? capturedFilter = null;
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactSelectItemDto>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            // Act
            await Service.GetIntroducerCandidatesAsync(excludeId);

            // Assert
            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filterFunc = capturedFilter.Compile();
            Assert.False(filterFunc(new Contact { Id = excludeId, IsPartial = false }));
            Assert.True(filterFunc(new Contact { Id = Guid.NewGuid(), IsPartial = false }));
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
        public async Task GetIntroducerCandidatesAsync_WhenNoCandidatesExist_ReturnsEmptyList()
        {
            // Arrange
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            List<ContactSelectItemDto> result = await Service.GetIntroducerCandidatesAsync(null);

            // Assert
            Assert.Empty(result);
        }
    }

    public class ContactReadServiceHowWeMetTests : ContactReadServiceTestBase
    {
        [Fact]
        public async Task GetContactFormAsyncRoundTripsHowWeMetFields()
        {
            Guid contactId = Guid.NewGuid();
            Guid introducerId = Guid.NewGuid();
            DateOnly firstMet = new(2024, 6, 15);
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Jane",
                LastName = "Doe",
                HowWeMet = "Met at a conference in Berlin.",
                FirstMetOn = firstMet,
                IntroducedByContactId = introducerId
            };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await Service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal("Met at a conference in Berlin.", result.HowWeMet);
            Assert.Equal(firstMet, result.FirstMetOn);
            Assert.Equal(introducerId, result.IntroducedByContactId);
        }

        [Fact]
        public async Task GetContactFormAsyncExcludesCurrentContactFromIntroducerCandidates()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Self", LastName = "Person" };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            Expression<Func<Contact, bool>>? capturedFilter = null;
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, ContactSelectItemDto>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, ContactSelectItemDto>>>(),
                It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Contact, bool>>, Expression<Func<Contact, ContactSelectItemDto>>, CancellationToken>(
                    (filter, _, _) => capturedFilter = filter)
                .ReturnsAsync([]);

            await Service.GetContactFormAsync(contactId);

            Assert.NotNull(capturedFilter);
            Func<Contact, bool> filterFunc = capturedFilter.Compile();
            Assert.False(filterFunc(new Contact { Id = contactId, IsPartial = false }));
            Assert.True(filterFunc(new Contact { Id = Guid.NewGuid(), IsPartial = false }));
            Assert.False(filterFunc(new Contact { Id = Guid.NewGuid(), IsPartial = true }));
        }

        [Fact]
        public async Task GetContactDetailsAsyncPopulatesIntroducedByContactNameWhenIntroducerExists()
        {
            Guid contactId = Guid.NewGuid();
            Guid introducerId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Bob",
                LastName = "Newcomer",
                IntroducedByContactId = introducerId
            };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Co-fetch lookup returns the introducer's projected entity.
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Contact>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Contact { Id = introducerId, FirstName = "Alice", LastName = "Mentor" }]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(introducerId, result.IntroducedByContactId);
            Assert.Equal("Alice Mentor", result.IntroducedByContactName);
        }

        [Fact]
        public async Task GetContactDetailsAsyncLeavesIntroducedByContactNameNullWhenIntroducerMissing()
        {
            Guid contactId = Guid.NewGuid();
            Guid introducerId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Carol",
                LastName = "Orphan",
                IntroducedByContactId = introducerId
            };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Co-fetch lookup returns nothing — referenced contact does not exist (e.g. deleted).
            RepositoryMock.Setup(r => r.ListProjectedAsync<Contact, Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Contact>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal(introducerId, result.IntroducedByContactId);
            Assert.Null(result.IntroducedByContactName);
        }

        [Fact]
        public async Task GetContactDetailsAsyncLeavesIntroducedByContactNameNullWhenNoneSet()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new()
            {
                Id = contactId,
                FirstName = "Lone",
                LastName = "Contact",
                IntroducedByContactId = null
            };

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            ContactDetailDto? result = await Service.GetContactDetailsAsync(contactId);

            Assert.NotNull(result);
            Assert.Null(result.IntroducedByContactName);
        }
    }
}
