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

public class ContactManagementServiceTests
{
    /// <summary>
    /// Shared scaffolding for nested test classes — constructs a <see cref="ContactManagementService"/>
    /// backed by Moq'd dependencies.
    /// </summary>
    public abstract class ContactManagementServiceTestBase
    {
        protected Mock<IRepository> RepositoryMock { get; }
        protected Mock<IFileValidationService> FileValidationServiceMock { get; }
        protected Mock<ISelfContactService> SelfContactServiceMock { get; }
        protected ContactManagementService Service { get; }

        protected ContactManagementServiceTestBase()
        {
            RepositoryMock = new Mock<IRepository>();
            FileValidationServiceMock = new Mock<IFileValidationService>();
            SelfContactServiceMock = new Mock<ISelfContactService>();
            SelfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);
            Service = new ContactManagementService(RepositoryMock.Object, FileValidationServiceMock.Object, SelfContactServiceMock.Object);
        }
    }

    public class ContactManagementServiceDeleteTests : ContactManagementServiceTestBase
    {
        [Fact]
        public async Task DeleteContactAsyncPartialContactOrphanedAfterDeleteIsCascadeDeleted()
        {
            Guid fullContactId = Guid.NewGuid();
            Guid partialContactId = Guid.NewGuid();

            Contact partialContact = new() { Id = partialContactId, IsPartial = true };

            Relationship relationship = new()
            {
                ContactId = fullContactId,
                RelatedContactId = partialContactId
            };

            RepositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(
                It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([relationship]);

            // ListByChunkedContainsAsync(asNoTracking:false) routes to ListAsync(3-arg)
            RepositoryMock.Setup(r => r.ListAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                    new List<Contact> { partialContact }.Where(predicate.Compile()).ToList());

            // No remaining relationships after the full contact is deleted — partial is orphaned
            RepositoryMock.Setup(r => r.ListAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            await Service.DeleteContactAsync(fullContactId);

            RepositoryMock.Verify(r => r.DeleteAsync<Contact>(partialContactId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteContactAsyncShouldNotUpdateUsersInLoop()
        {
            Guid contactId = Guid.NewGuid();
            List<Core.Models.User> users =
            [
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId },
                new Rvnx.CRM.Core.Models.User { Id = Guid.NewGuid(), SelfContactId = contactId }
            ];

            RepositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            RepositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            await Service.DeleteContactAsync(contactId);

            RepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Rvnx.CRM.Core.Models.User>(), It.IsAny<CancellationToken>()), Times.Never);
            RepositoryMock.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Rvnx.CRM.Core.Models.User>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class ImageTests : ContactManagementServiceTestBase
    {
        private readonly List<Attachment> _attachments = [];
        private readonly List<ContactMethod> _contactMethods = [];
        private readonly List<SignificantDate> _significantDates = [];

        public ImageTests()
        {
            RepositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Attachment, bool>> predicate, CancellationToken ct) =>
                {
                    return _attachments.AsQueryable().Where(predicate).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(
                It.IsAny<Expression<Func<ContactMethod, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ContactMethod, bool>> predicate, CancellationToken ct) =>
                {
                    return _contactMethods.AsQueryable().Where(predicate).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(
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

            ContactOperationResult result = await Service.UnsetProfilePhotoAsync(contactId);

            Assert.True(result.Success);
            RepositoryMock.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<Attachment>>(list => list.Count() == 2), It.IsAny<CancellationToken>()), Times.Once());
            RepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Never());
            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoAsyncArchivesMultipleExistingProfilePhotos()
        {
            Guid contactId = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();

            Attachment existingPhoto1 = new() { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
            Attachment existingPhoto2 = new() { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
            _attachments.Add(existingPhoto1);
            _attachments.Add(existingPhoto2);

            Attachment newAttachment = new() { Id = attachmentId, ContactId = contactId, FileName = "photo.jpg", ContentType = "image/jpeg", AttachmentType = AttachmentTypes.General };
            RepositoryMock.Setup(r => r.GetByIdAsync<Attachment>(attachmentId, It.IsAny<CancellationToken>())).ReturnsAsync(newAttachment);
            FileValidationServiceMock.Setup(f => f.IsImageExtension(".jpg")).Returns(true);

            ContactOperationResult result = await Service.SetAttachmentAsProfilePhotoAsync(contactId, attachmentId);

            Assert.True(result.Success);
            RepositoryMock.Verify(
                r => r.UpdateRangeAsync(
                    It.Is<IEnumerable<Attachment>>(list => list.Count() == 2 && list.Contains(existingPhoto1) && list.Contains(existingPhoto2)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoAsyncReturnsNotFoundWhenAttachmentDoesNotExist()
        {
            Guid contactId = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();

            ContactOperationResult result = await Service.SetAttachmentAsProfilePhotoAsync(contactId, attachmentId);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoAsyncReturnsNotFoundWhenAttachmentBelongsToDifferentContact()
        {
            Guid contactId = Guid.NewGuid();
            Guid otherContactId = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();

            Attachment attachment = new() { Id = attachmentId, ContactId = otherContactId, FileName = "test.png", ContentType = "image/png" };
            RepositoryMock.Setup(r => r.GetByIdAsync<Attachment>(attachmentId, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

            ContactOperationResult result = await Service.SetAttachmentAsProfilePhotoAsync(contactId, attachmentId);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoAsyncReturnsFailureWhenAttachmentIsNotAnImage()
        {
            Guid contactId = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();

            Attachment attachment = new() { Id = attachmentId, ContactId = contactId, FileName = "test.txt", ContentType = "text/plain" };
            RepositoryMock.Setup(r => r.GetByIdAsync<Attachment>(attachmentId, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

            FileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(false);

            ContactOperationResult result = await Service.SetAttachmentAsProfilePhotoAsync(contactId, attachmentId);

            Assert.False(result.Success);
            Assert.Contains("Attachment is not an image.", result.Errors);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoAsyncSucceedsWhenValid()
        {
            Guid contactId = Guid.NewGuid();
            Guid attachmentId = Guid.NewGuid();

            Attachment existingProfilePic = new() { Id = Guid.NewGuid(), ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
            _attachments.Add(existingProfilePic);

            Attachment attachment = new() { Id = attachmentId, ContactId = contactId, FileName = "test.png", ContentType = "image/png", AttachmentType = AttachmentTypes.General };
            RepositoryMock.Setup(r => r.GetByIdAsync<Attachment>(attachmentId, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

            FileValidationServiceMock.Setup(f => f.IsImageExtension(".png")).Returns(true);

            ContactOperationResult result = await Service.SetAttachmentAsProfilePhotoAsync(contactId, attachmentId);

            Assert.True(result.Success);
            Assert.Equal(contactId, result.ContactId);

            Assert.Equal(AttachmentTypes.General, existingProfilePic.AttachmentType);
            RepositoryMock.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<Attachment>>(list => list.Contains(existingProfilePic)), It.IsAny<CancellationToken>()), Times.Once());

            Assert.Equal(AttachmentTypes.ProfileImage, attachment.AttachmentType);
            RepositoryMock.Verify(r => r.UpdateAsync(attachment, It.IsAny<CancellationToken>()), Times.Once());
            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateContactWithValidImageAddsAttachment()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            FileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            FileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            string fileName = "test.jpg";
            string contentType = "image/jpeg";
            byte[] fileContent = [1, 2, 3];
            using MemoryStream stream = new(fileContent);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            Assert.True(result.Success);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<Attachment>(a =>
                a.ContactId == contactId &&
                a.AttachmentType == AttachmentTypes.ProfileImage &&
                a.FileName == fileName &&
                a.ContentType == contentType),
                It.IsAny<CancellationToken>()), Times.Once);
        }

    }
    public class OptimizationTests : ContactManagementServiceTestBase
    {
        private int _relationshipListCalls;

        [Fact]
        public async Task DeleteContactAsyncShouldReduceRoundTrips()
        {
            Guid contactId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    _relationshipListCalls++;
                    return [];
                });

            RepositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            await Service.DeleteContactAsync(contactId);

            Assert.Equal(1, _relationshipListCalls);

            RepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

    }
    public class PerformanceTests : ContactManagementServiceTestBase
    {
        private int _relationshipListCalls;
        private int _contactListCalls;

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
                ContactId = p.Id,
                RelatedContactId = Guid.NewGuid() // Unique sibling
            }).ToList();

            // Initial Relationships (Deleted contact <-> Partial contacts)
            List<Relationship> initialRelationships = partialContacts.Select(p => new Relationship
            {
                ContactId = contactId,
                RelatedContactId = p.Id
            }).ToList();

            List<Relationship> allRelationships = [.. initialRelationships, .. siblingRelationships];

            List<Contact> allContacts =
            [
                .. partialContacts,
                    .. siblingRelationships.Select(r => new Contact { Id = r.RelatedContactId, IsPartial = false }),
                ];


            RepositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    _relationshipListCalls++;
                    Func<Relationship, bool> func = predicate.Compile();
                    return allRelationships.Where(func).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            RepositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                 {
                     _contactListCalls++;
                     Func<Contact, bool> func = predicate.Compile();
                     return allContacts.Where(func).ToList();
                 });

            // Also mock dependencies deletion calls to avoid null ref if we accidentally delete something
            RepositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);


            await Service.DeleteContactAsync(contactId);

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

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = true };

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(d => d.ContactId == contactId && d.EventDate == new DateOnly(1990, 1, 1) && d.Title == Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday), It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(o => o.DaysBeforeEvent == 0 && o.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsyncWithExistingBirthdayUpdatesDate()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1995, 5, 5) };

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.Equal(new DateOnly(1995, 5, 5), existingDate.EventDate);
            RepositoryMock.Verify(r => r.UpdateAsync(existingDate, It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncWithRemovedBirthdayDeletesDate()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = null };

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            RepositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(dateId, It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncTogglingReminderOnActivatesOffset()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), SignificantDateId = dateId, DaysBeforeEvent = 0, IsActive = false };

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ReminderOffset, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<ReminderOffset> { existingOffset }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = true };

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.True(existingOffset.IsActive);
            RepositoryMock.Verify(r => r.UpdateAsync(existingOffset, It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactAsyncTogglingReminderOffDeactivatesOffset()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Guid dateId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            SignificantDate existingDate = new() { Id = dateId, ContactId = contactId, EventDate = new DateOnly(1990, 1, 1), Title = Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday };

            ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), SignificantDateId = dateId, DaysBeforeEvent = 0, IsActive = true };

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<SignificantDate> { existingDate }.Where(predicate.Compile()).ToList();
                });

            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ReminderOffset, bool>> predicate, CancellationToken ct) =>
                {
                    return new List<ReminderOffset> { existingOffset }.Where(predicate.Compile()).ToList();
                });

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User", Birthday = new DateTime(1990, 1, 1), RemindOnBirthday = false };

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.False(existingOffset.IsActive);
            RepositoryMock.Verify(r => r.UpdateAsync(existingOffset, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactAsyncThrowsConcurrencyExceptionWhenContactExistsReturnsFailure()
        {
            Guid contactId = Guid.NewGuid();
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new() { FirstName = "Updated" };

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Rvnx.CRM.Core.Exceptions.EntityConcurrencyException("Concurrency conflict"));

            RepositoryMock.Setup(r => r.ExistsAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.False(result.Success);
            Assert.False(result.IsNotFound);
            Assert.Contains("The contact was modified by another user. Please reload and try again.", result.Errors);
        }

        [Fact]
        public async Task UpdateContactAsyncThrowsConcurrencyExceptionWhenContactDeletedReturnsNotFound()
        {
            Guid contactId = Guid.NewGuid();
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new() { FirstName = "Updated" };

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);

            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            RepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Rvnx.CRM.Core.Exceptions.EntityConcurrencyException("Concurrency conflict"));

            RepositoryMock.Setup(r => r.ExistsAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task CreateContactAsyncUsesSingleSaveChangesAsync()
        {
            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<System.Linq.Expressions.Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto dto = new()
            {
                FirstName = "Performance",
                LastName = "Test",
                Email = "perf@example.com",
                Phone = "212-736-5000",
                Birthday = new DateTime(1990, 1, 1)
            };

            await Service.CreateContactAsync(dto);

            // Verifies that only ONE SaveChangesAsync is called, reducing DB roundtrips
            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Email and Phone
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Once); // Birthday
        }
    }

    public class SelfContactDeceasedGuardTests : ContactManagementServiceTestBase
    {
        [Fact]
        public async Task UpdateContactAsyncWhenContactIsSelfContactCoercesDeceasedFieldsToFalse()
        {
            // Defense-in-depth at the service boundary so the API PUT/PATCH paths
            // (which don't run the MVC controller's coercion) cannot mark the user's
            // own self-contact deceased.
            Guid contactId = Guid.NewGuid();
            Contact existingContact = new() { Id = contactId, IsDeceased = false };
            ContactFormDto dto = new()
            {
                FirstName = "Me",
                IsDeceased = true,
                DateOfDeath = new DateOnly(2030, 1, 1)
            };

            SelfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(contactId);

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);
            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.False(existingContact.IsDeceased);
            Assert.Null(existingContact.DateOfDeath);
            Assert.False(dto.IsDeceased);
            Assert.Null(dto.DateOfDeath);
        }

        [Fact]
        public async Task UpdateContactAsyncWhenContactIsNotSelfContactKeepsDeceasedFields()
        {
            Guid contactId = Guid.NewGuid();
            Guid otherSelfId = Guid.NewGuid();
            DateOnly dod = new(2024, 6, 15);
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new()
            {
                FirstName = "Friend",
                IsDeceased = true,
                DateOfDeath = dod
            };

            SelfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(otherSelfId);

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);
            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.True(existingContact.IsDeceased);
            Assert.Equal(dod, existingContact.DateOfDeath);
        }

        [Fact]
        public async Task UpdateContactAsyncWhenNoSelfContactSetDoesNotCoerceDeceasedFields()
        {
            // If the user has no self-contact, GetSelfContactIdAsync returns null and the
            // coercion must NOT silently match every contact (Guid.Empty vs default).
            Guid contactId = Guid.NewGuid();
            DateOnly dod = new(2024, 6, 15);
            Contact existingContact = new() { Id = contactId };
            ContactFormDto dto = new()
            {
                FirstName = "Friend",
                IsDeceased = true,
                DateOfDeath = dod
            };

            SelfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContact);
            RepositoryMock.Setup(r => r.ListAsync<ContactMethod>(It.IsAny<Expression<Func<ContactMethod, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            RepositoryMock.Setup(r => r.ListAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await Service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            Assert.True(existingContact.IsDeceased);
            Assert.Equal(dod, existingContact.DateOfDeath);
        }
    }

    public class DemoteTests : ContactManagementServiceTestBase
    {
        [Fact]
        public async Task DemoteToPartialAsyncReturnsNotFoundWhenContactDoesNotExist()
        {
            Guid nonExistentId = Guid.NewGuid();

            RepositoryMock.Setup(r => r.GetByIdAsync<Contact>(nonExistentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact?)null);

            ContactOperationResult result = await Service.DemoteToPartialAsync(nonExistentId);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            RepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class CreateTests : ContactManagementServiceTestBase
    {

        [Fact]
        public async Task CreateContactAsyncWithValidDtoReturnsSuccessAndAddsEntities()
        {
            ContactFormDto dto = new()
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@example.com",
                Phone = "(212) 736-5000",
                Birthday = new DateTime(1985, 5, 15),
                RemindOnBirthday = true
            };

            RepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact c, CancellationToken ct) => c);

            RepositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<System.Linq.Expressions.Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactOperationResult result = await Service.CreateContactAsync(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.ContactId);

            RepositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Jane" && c.LastName == "Doe"), It.IsAny<CancellationToken>()), Times.Once);

            // Verify that ContactUpdateHelper was used to add methods and dates
            RepositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == Rvnx.CRM.Core.Enumerations.ContactMethodType.Email && cm.Value == dto.Email), It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == Rvnx.CRM.Core.Enumerations.ContactMethodType.Phone && cm.Value == "+12127365000"), It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd => sd.EventDate == DateOnly.FromDateTime(dto.Birthday.Value)), It.IsAny<CancellationToken>()), Times.Once);
            RepositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(ro => ro.DaysBeforeEvent == 0 && ro.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);

            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateContactAsyncWithMinimalDtoReturnsSuccessAndOnlyAddsContact()
        {
            ContactFormDto dto = new()
            {
                FirstName = "Minimal",
                LastName = "Contact"
            };

            RepositoryMock.Setup(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contact c, CancellationToken ct) => c);

            ContactOperationResult result = await Service.CreateContactAsync(dto);

            Assert.True(result.Success);
            Assert.NotNull(result.ContactId);

            RepositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Minimal" && c.LastName == "Contact"), It.IsAny<CancellationToken>()), Times.Once);

            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
            RepositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);

            RepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
