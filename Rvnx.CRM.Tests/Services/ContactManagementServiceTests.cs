using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactManagementServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        // In-memory collections to support ListAsync queries
        private readonly List<ContactMethod> _contactMethods = [];
        private readonly List<SignificantDate> _significantDates = [];
        private readonly List<Attachment> _attachments = [];
        private readonly List<Contact> _contacts = [];
        private readonly List<Relationship> _relationships = [];
        private readonly List<User> _users = [];

        public ContactManagementServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);

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

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Attachment, bool>> predicate, CancellationToken ct) =>
                {
                    return _attachments.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<User>(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate, CancellationToken ct) =>
                {
                    return _users.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    return _relationships.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Relationship>(
                It.IsAny<Expression<Func<Relationship, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    return _relationships.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    return _contacts.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Contact, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    return _contacts.AsQueryable().Where(predicate).ToList();
                });

            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            _repositoryMock.Setup(r => r.DeleteAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, CancellationToken>((id, ct) =>
                {
                    Contact? item = _contacts.FirstOrDefault(c => c.Id == id);
                    if (item != null)
                    {
                        _contacts.Remove(item);
                    }
                });

            _repositoryMock.Setup(r => r.DeleteAsync<ContactMethod>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, CancellationToken>((id, ct) =>
                {
                    ContactMethod? item = _contactMethods.FirstOrDefault(c => c.Id == id);
                    if (item != null)
                    {
                        _contactMethods.Remove(item);
                    }
                });

            _repositoryMock.Setup(r => r.DeleteAsync<SignificantDate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, CancellationToken>((id, ct) =>
                {
                    SignificantDate? item = _significantDates.FirstOrDefault(c => c.Id == id);
                    if (item != null)
                    {
                        _significantDates.Remove(item);
                    }
                });

            _repositoryMock.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Relationship>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<Relationship>, CancellationToken>((items, ct) =>
                {
                    foreach (Relationship item in items)
                    {
                        Relationship? existing = _relationships.FirstOrDefault(r => r.Id == item.Id);
                        if (existing != null)
                        {
                            _relationships.Remove(existing);
                        }
                    }
                });

            _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<Relationship, bool>>, CancellationToken>((predicate, ct) =>
                {
                    Func<Relationship, bool> func = predicate.Compile();
                    List<Relationship> itemsToRemove = _relationships.Where(func).ToList();
                    foreach (Relationship? item in itemsToRemove)
                    {
                        _relationships.Remove(item);
                    }
                });
        }

        [Fact]
        public async Task UpdateContactWithEmptyEmailDeletesExistingPrimaryEmail()
        {
            Guid contactId = Guid.NewGuid();
            ContactMethod existingEmail = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Type = ContactMethodType.Email,
                Value = "old@example.com",
                Label = ContactMethodLabels.Primary
            };
            _contactMethods.Add(existingEmail);

            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "User",
                Email = "" // Empty email
            };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(existingEmail.Id, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactWithNewEmailAddsContactMethod()
        {
            Guid contactId = Guid.NewGuid();

            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "User",
                Email = "new@example.com"
            };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm =>
                cm.ContactId == contactId &&
                cm.Type == ContactMethodType.Email &&
                cm.Value == "new@example.com" &&
                cm.Label == ContactMethodLabels.Primary),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactWithChangedEmailUpdatesExistingContactMethod()
        {
            Guid contactId = Guid.NewGuid();
            ContactMethod existingEmail = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Type = ContactMethodType.Email,
                Value = "old@example.com",
                Label = ContactMethodLabels.Primary
            };
            _contactMethods.Add(existingEmail);

            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "User",
                Email = "new@example.com"
            };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);

            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ContactMethod>(cm =>
                cm.Id == existingEmail.Id &&
                cm.Value == "new@example.com"),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal("new@example.com", existingEmail.Value); // Confirm side effect
        }

        [Fact]
        public async Task UpdateContactWithEmptyBirthdayDeletesExistingBirthday()
        {
            Guid contactId = Guid.NewGuid();
            SignificantDate existingBirthday = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = SignificantDateTitles.Birthday,
                Date = new DateTime(1990, 1, 1)
            };
            _significantDates.Add(existingBirthday);

            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "User",
                Birthday = null
            };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(existingBirthday.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactWithNewBirthdayAddsSignificantDate()
        {
            Guid contactId = Guid.NewGuid();

            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            DateTime newBirthday = new(2000, 5, 20);
            ContactFormDto dto = new()
            {
                Id = contactId,
                FirstName = "Test",
                LastName = "User",
                Birthday = newBirthday
            };

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd =>
                sd.ContactId == contactId &&
                sd.Title == SignificantDateTitles.Birthday &&
                sd.Date == newBirthday &&
                sd.RemindMe == true && // Default behavior check
                sd.EventFrequency == TimeSpan.FromDays(365)), // Default behavior check
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactWithInvalidImageExtensionReturnsFailure()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            using MemoryStream stream = new();
            string fileName = "test.txt";
            string contentType = "text/plain";

            _fileValidationServiceMock.Setup(f => f.IsImageExtension(".txt")).Returns(false);

            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            Assert.False(result.Success);
            Assert.Contains("Only image files", result.Errors.First());
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactWithNewImageArchivesOldProfilePhoto()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            Attachment existingProfilePhoto = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AttachmentType = AttachmentTypes.ProfileImage,
                FileName = "old.jpg"
            };
            _attachments.Add(existingProfilePhoto);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            using MemoryStream stream = new();
            string fileName = "new.jpg";
            string contentType = "image/jpeg";

            _fileValidationServiceMock.Setup(f => f.IsImageExtension(".jpg")).Returns(true);
            _fileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), ".jpg")).Returns(true);

            await _service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Attachment>(a => a.Id == existingProfilePhoto.Id && a.AttachmentType == "General"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Attachment>(a => a.AttachmentType == AttachmentTypes.ProfileImage && a.FileName == fileName), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UnsetProfilePhotoArchivesExistingPhoto()
        {
            Guid contactId = Guid.NewGuid();
            Attachment existingProfilePhoto = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AttachmentType = AttachmentTypes.ProfileImage,
                FileName = "profile.jpg"
            };
            _attachments.Add(existingProfilePhoto);

            ContactOperationResult result = await _service.UnsetProfilePhotoAsync(contactId);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Attachment>(a => a.Id == existingProfilePhoto.Id && a.AttachmentType == "General"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoArchivesOldAndSetsNew()
        {
            Guid contactId = Guid.NewGuid();
            Attachment existingProfilePhoto = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AttachmentType = AttachmentTypes.ProfileImage,
                FileName = "old.jpg"
            };
            Attachment newPhoto = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AttachmentType = "General",
                FileName = "new.jpg",
                ContentType = "image/jpeg"
            };
            _attachments.Add(existingProfilePhoto);
            _attachments.Add(newPhoto);

            _repositoryMock.Setup(r => r.GetByIdAsync<Attachment>(newPhoto.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newPhoto);

            ContactOperationResult result = await _service.SetAttachmentAsProfilePhotoAsync(contactId, newPhoto.Id);

            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Attachment>(a => a.Id == existingProfilePhoto.Id && a.AttachmentType == "General"), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Attachment>(a => a.Id == newPhoto.Id && a.AttachmentType == AttachmentTypes.ProfileImage), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoFailsForNonImage()
        {
            Guid contactId = Guid.NewGuid();
            Attachment doc = new()
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AttachmentType = "General",
                FileName = "doc.pdf",
                ContentType = "application/pdf"
            };
            _attachments.Add(doc);

            _repositoryMock.Setup(r => r.GetByIdAsync<Attachment>(doc.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            _fileValidationServiceMock.Setup(f => f.IsImageExtension(".pdf")).Returns(false);

            ContactOperationResult result = await _service.SetAttachmentAsProfilePhotoAsync(contactId, doc.Id);

            Assert.False(result.Success);
            Assert.Contains("not an image", result.Errors.First());
        }

        [Fact]
        public async Task SetAttachmentAsProfilePhotoFailsForWrongContact()
        {
            Guid contactId = Guid.NewGuid();
            Guid otherContactId = Guid.NewGuid();
            Attachment photo = new()
            {
                Id = Guid.NewGuid(),
                ContactId = otherContactId,
                AttachmentType = "General",
                FileName = "photo.jpg",
                ContentType = "image/jpeg"
            };
            _attachments.Add(photo);

            _repositoryMock.Setup(r => r.GetByIdAsync<Attachment>(photo.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);

            ContactOperationResult result = await _service.SetAttachmentAsProfilePhotoAsync(contactId, photo.Id);

            Assert.True(result.IsNotFound);
        }
        [Fact]
        public async Task DeleteContactDeletesContactAndDirectDependencies()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "ToDelete" };
            _contacts.Add(contact);

            Relationship rel1 = new() { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = Guid.NewGuid(), EntityType = EntityTypes.Person };
            Relationship rel2 = new() { Id = Guid.NewGuid(), EntityId = Guid.NewGuid(), RelatedEntityId = contactId, EntityType = EntityTypes.Person };
            _relationships.Add(rel1);
            _relationships.Add(rel2);

            await _service.DeleteContactAsync(contactId);

            // 1. Deletes relationships (Using optimized DeleteAsync(predicate))
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            _repositoryMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Relationship>>(), It.IsAny<CancellationToken>()), Times.Never);

            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(contactId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteContactAsyncOrphansPartialContactDeletesIt()
        {
            Guid contactId = Guid.NewGuid();
            Guid partialContactId = Guid.NewGuid();

            Contact contact = new() { Id = contactId, FirstName = "Main", IsPartial = false };
            Contact partialContact = new() { Id = partialContactId, FirstName = "Partial", IsPartial = true };

            _contacts.Add(contact);
            _contacts.Add(partialContact);

            Relationship rel = new() { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person };
            _relationships.Add(rel);

            await _service.DeleteContactAsync(contactId);

            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(contactId, It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(partialContactId, It.IsAny<CancellationToken>()), Times.Once); // The orphan must be deleted
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task DeleteContactAsyncDoesNotDeletePartialContactIfStillLinked()
        {
            Guid contactId = Guid.NewGuid();
            Guid partialContactId = Guid.NewGuid();
            Guid otherFullContactId = Guid.NewGuid();

            Contact contact = new() { Id = contactId, FirstName = "Main", IsPartial = false };
            Contact partialContact = new() { Id = partialContactId, FirstName = "Partial", IsPartial = true };
            Contact otherContact = new() { Id = otherFullContactId, FirstName = "Other", IsPartial = false };

            _contacts.Add(contact);
            _contacts.Add(partialContact);
            _contacts.Add(otherContact);

            Relationship rel1 = new() { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person };
            Relationship rel2 = new() { Id = Guid.NewGuid(), EntityId = otherFullContactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person };

            _relationships.Add(rel1);
            _relationships.Add(rel2);

            await _service.DeleteContactAsync(contactId);

            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(contactId, It.IsAny<CancellationToken>()), Times.Once);

            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(partialContactId, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteContactUnlinksSelfContactFromUser()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Linked", LastName = "Contact" };
            User user = new()
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                SelfContactId = contactId
            };

            _contacts.Add(contact);
            _users.Add(user);

            await _service.DeleteContactAsync(contactId);

            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Id == user.Id && u.SelfContactId == null), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
