using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactManagementServiceTests
{
    public class ImageTests
    {

        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;
        private readonly List<Attachment> _attachments = [];
        private readonly List<ContactMethod> _contactMethods = [];
        private readonly List<SignificantDate> _significantDates = [];

        public ImageTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Attachment, bool>> predicate, CancellationToken ct) =>
                {
                    return _attachments.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(
                It.IsAny<Expression<Func<ContactMethod, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ContactMethod, bool>> predicate, CancellationToken ct) =>
                {
                    return _contactMethods.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return _significantDates.AsQueryable().Where(predicate).ToList();
                });
        }

        [Fact]
        public async Task UnsetProfilePhotoAsyncUsesSingleUpdateRangeAsync()
        {
            Guid contactId = Guid.NewGuid();
            Attachment existingAttachment1 = new() { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
            Attachment existingAttachment2 = new() { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };

            _attachments.Add(existingAttachment1);
            _attachments.Add(existingAttachment2);

            ContactOperationResult result = await _service.UnsetProfilePhotoAsync(contactId);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<Attachment>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once());
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Never());
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateContactWithValidImageAddsAttachment()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            _fileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            _fileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            string fileName = "test.jpg";
            string contentType = "image/jpeg";
            byte[] fileContent = [1, 2, 3];
            using MemoryStream stream = new(fileContent);

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Attachment>(a =>
                a.ContactId == contactId &&
                a.AttachmentType == AttachmentTypes.ProfileImage &&
                a.FileName == fileName &&
                a.ContentType == contentType),
                It.IsAny<CancellationToken>()), Times.Once);
        }

    }
    public class OptimizationTests
    {

        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        private int _relationshipListCalls;

        public OptimizationTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task DeleteContactAsyncShouldReduceRoundTrips()
        {
            Guid contactId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    _relationshipListCalls++;
                    return [];
                });

            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            await _service.DeleteContactAsync(contactId);

            // 1. Initial Relationship fetch -> ListAsync (1 call)
            // 2. DeleteRelatedEntitiesAsync<Relationship> -> DeleteAsync(predicate) (NO ListAsync)
            // 3. relatedTo -> DeleteAsync(predicate) (NO ListAsync)

            Assert.Equal(1, _relationshipListCalls);

            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

    }
    public class PerformanceTests
    {

        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        // Counters for calls
        private int _relationshipListCalls;
        private int _contactListCalls;

        public PerformanceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task DeleteContactAsyncWithManyPartialContactsShouldOptimizeQueries()
        {
            int partialContactCount = 10;
            Guid contactId = Guid.NewGuid();

            List<Contact> partialContacts = Enumerable.Range(0, partialContactCount)
                .Select(i => new Contact { Id = Guid.NewGuid(), IsPartial = true })
                .ToList();

            // Each partial contact is related to a "Full" contact (sibling), ensuring they are NOT orphans.
            List<Relationship> siblingRelationships = partialContacts.Select(p => new Relationship
            {
                EntityId = p.Id,
                RelatedEntityId = Guid.NewGuid(), // Unique sibling
                EntityType = EntityTypes.Person
            }).ToList();

            // Initial Relationships (Deleted contact <-> Partial contacts)
            List<Relationship> initialRelationships = partialContacts.Select(p => new Relationship
            {
                EntityId = contactId,
                RelatedEntityId = p.Id,
                EntityType = EntityTypes.Person
            }).ToList();

            List<Relationship> allRelationships = [.. initialRelationships, .. siblingRelationships];

            List<Contact> allContacts =
            [
                .. partialContacts,
                    .. siblingRelationships.Select(r => new Contact { Id = r.RelatedEntityId, IsPartial = false }),
                ];


            _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            // Also mock dependencies deletion calls to avoid null ref if we accidentally delete something
            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);


            await _service.DeleteContactAsync(contactId);

            // 1. Initial Relationship fetch (1 call, 2-arg)
            // 2. Partial contacts fetch (1 call, Contact 3-arg)
            // 3. Batch Relationship fetch (1 call, 3-arg)
            // 4. Batch Sibling fetch (1 call, Contact 3-arg)

            // Total Relationship Calls = 1 + 1 = 2.
            // But we also have calls in DeleteContactDependenciesAsync(contactId) at the start.
            // DeleteContactDependenciesAsync(contactId):
            //    - DeleteRelatedEntitiesAsync -> ListAsync<Relationship> (2-arg) -> +1
            //    - relatedTo -> ListAsync<Relationship> (2-arg) -> +1

            // So Total Relationship Calls = 1 (initial) + 2 (delete deps) + 1 (bulk partial rels) = 4.

            // Total Contact Calls = 1 (partial contacts) + 1 (siblings) = 2.

            // If fallback deletion happened (bug), we'd have 20 more relationship calls.

            Assert.True(_relationshipListCalls <= 4, $"Expected <= 4 relationship calls, but got {_relationshipListCalls}");
            Assert.True(_contactListCalls <= 2, $"Expected <= 2 contact calls, but got {_contactListCalls}");
        }

        [Fact]
        public async Task UpdateContactAsyncWithNewBirthdayAddsDateAndOffset()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = true };

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(d => d.ContactId == contactId && d.EventDate == new DateOnly(1990, 1, 1) && d.Title == Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(o => o.DaysBeforeEvent == 0 && o.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsyncWithExistingBirthdayUpdatesDate()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1995, 5, 5) };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.Equal(new DateOnly(1995, 5, 5), existingDate.EventDate);
            _repositoryMock.Verify(r => r.UpdateAsync(existingDate, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncWithRemovedBirthdayDeletesDate()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = null };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(dateId, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncTogglingReminderOnActivatesOffset()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), SignificantDateId = dateId, DaysBeforeEvent = 0, IsActive = false };

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ReminderOffset, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<ReminderOffset> { existingOffset }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = true };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.True(existingOffset.IsActive);
            _repositoryMock.Verify(r => r.UpdateAsync(existingOffset, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncTogglingReminderOffDeactivatesOffset()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), SignificantDateId = dateId, DaysBeforeEvent = 0, IsActive = true };

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ReminderOffset, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<ReminderOffset> { existingOffset }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = false };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.False(existingOffset.IsActive);
            _repositoryMock.Verify(r => r.UpdateAsync(existingOffset, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsyncThrowsConcurrencyExceptionWhenContactExistsReturnsFailure()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new() { FirstName = "Updated" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Rvnx.CRM.Core.Exceptions.EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.False(result.IsNotFound);
            Assert.Contains("The contact was modified by another user. Please reload and try again.", result.Errors);
        }

        [Fact]
        public async Task UpdateContactAsyncThrowsConcurrencyExceptionWhenContactDeletedReturnsNotFound()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new() { FirstName = "Updated" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);

            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Rvnx.CRM.Core.Exceptions.EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task CreateContactAsyncUsesSingleSaveChangesAsync()
        {
            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<System.Linq.Expressions.Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto dto = new()
            {
                FirstName = "Performance",
                LastName = "Test",
                Email = "perf@example.com",
                Phone = "123456789",
                Birthday = new DateTime(1990, 1, 1)
            };

            await _service.CreateContactAsync(dto);

            // Verifies that only ONE SaveChangesAsync is called, reducing DB roundtrips
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Email and Phone
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Once); // Birthday
        }
    }

    public class CreateTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        public CreateTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);
        }

        [Fact]
        public async Task CreateContactAsyncWithValidDtoReturnsSuccessAndAddsEntities()
        {
            // Arrange
            ContactFormDto dto = new()
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                Phone = "+1234567890",
                Birthday = new DateTime(1985, 5, 15),
                RemindOnBirthday = true
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact c, CancellationToken ct) => c);

            _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<System.Linq.Expressions.Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            ContactOperationResult result = await _service.CreateContactAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ContactId);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Jane" && c.LastName == "Doe"), It.IsAny<CancellationToken>()), Times.Once);

            // Verify that ContactUpdateHelper was used to add methods and dates
            _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == Rvnx.CRM.Core.Enumerations.ContactMethodType.Email && cm.Value == dto.Email), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == Rvnx.CRM.Core.Enumerations.ContactMethodType.Phone && cm.Value == dto.Phone), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd => sd.EventDate == DateOnly.FromDateTime(dto.Birthday.Value)), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(ro => ro.DaysBeforeEvent == 0 && ro.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsyncWithMinimalDtoReturnsSuccessAndOnlyAddsContact()
        {
            // Arrange
            ContactFormDto dto = new()
            {
                FirstName = "Minimal",
                LastName = "Contact"
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact c, CancellationToken ct) => c);

            // Act
            ContactOperationResult result = await _service.CreateContactAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ContactId);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Minimal" && c.LastName == "Contact"), It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}