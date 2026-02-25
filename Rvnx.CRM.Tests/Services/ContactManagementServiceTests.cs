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
    public class ContactManagementServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;

        // In-memory collections to support ListAsync queries
        private readonly List<ContactMethod> _contactMethods = [];
        private readonly List<SignificantDate> _significantDates = [];
        private readonly List<Attachment> _attachments = [];

        public ContactManagementServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);

            // Setup ListAsync for ContactMethod
            _repositoryMock.Setup(r => r.ListAsync<ContactMethod>(
                It.IsAny<Expression<Func<ContactMethod, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<ContactMethod, bool>> predicate, CancellationToken ct) =>
                {
                    return _contactMethods.AsQueryable().Where(predicate).ToList();
                });

            // Setup ListAsync for SignificantDate
            _repositoryMock.Setup(r => r.ListAsync<SignificantDate>(
                It.IsAny<Expression<Func<SignificantDate, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SignificantDate, bool>> predicate, CancellationToken ct) =>
                {
                    return _significantDates.AsQueryable().Where(predicate).ToList();
                });

            // Setup ListAsync for Attachment
            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Attachment, bool>> predicate, CancellationToken ct) =>
                {
                    return _attachments.AsQueryable().Where(predicate).ToList();
                });
        }

        [Fact]
        public async Task UpdateContactWithEmptyEmailDeletesExistingPrimaryEmail()
        {
            // Arrange
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

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(existingEmail.Id, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateContactWithNewEmailAddsContactMethod()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            // No existing email in _contactMethods

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

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
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
            // Arrange
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

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
            Assert.True(result.Success);

            // Verify UpdateAsync was called on the SAME object reference (since we're modifying it in memory)
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ContactMethod>(cm =>
                cm.Id == existingEmail.Id &&
                cm.Value == "new@example.com"),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal("new@example.com", existingEmail.Value); // Confirm side effect
        }

        [Fact]
        public async Task UpdateContactWithEmptyBirthdayDeletesExistingBirthday()
        {
            // Arrange
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

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(existingBirthday.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateContactWithNewBirthdayAddsSignificantDate()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            // No existing birthday

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

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, null, null, null);

            // Assert
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
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            using MemoryStream stream = new();
            string fileName = "test.txt";
            string contentType = "text/plain";

            _fileValidationServiceMock.Setup(f => f.IsImageExtension(".txt")).Returns(false);

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Only image files", result.Errors.First());
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task DeleteContactAsyncOrphansPartialContactDeletesIt()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid partialContactId = Guid.NewGuid();

            // Mock ListAsync<User> (no users)
            _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Mock Relationships (deleting contact has relationship to partial contact)
            List<Relationship> initialRelationships = [
                new Relationship { EntityId = contactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person }
            ];

            // Setup ListAsync<Relationship> (2-arg)
            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialRelationships);

            // Setup ListAsync<Relationship> (3-arg) used by bulk query
            _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(initialRelationships);

            // Mock Dependencies (empty)
            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // When listing linked contacts to see if they are partial
            List<Contact> linkedContacts = [
                new Contact { Id = partialContactId, IsPartial = true }
            ];

            var contactMockFunc = new Func<Expression<Func<Contact, bool>>, CancellationToken, string[], List<Contact>>((predicate, ct, includes) =>
            {
                Func<Contact, bool> func = predicate.Compile();
                return linkedContacts.Where(func).ToList();
            });

            _repositoryMock.Setup(r => r.ListAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contactMockFunc);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contactMockFunc);

            // Act
            await _service.DeleteContactAsync(contactId);

            // Assert
            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(contactId, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(partialContactId, It.IsAny<CancellationToken>()), Times.Once); // The orphan must be deleted
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task DeleteContactAsyncDoesNotDeletePartialContactIfStillLinked()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid partialContactId = Guid.NewGuid();
            Guid otherFullContactId = Guid.NewGuid();

            // Mock ListAsync<User> (no users)
            _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Data
            List<Relationship> initialRelationships = [
                new Relationship { EntityId = contactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person }
            ];
            List<Relationship> partialContactRelationships = [
                new Relationship { EntityId = otherFullContactId, RelatedEntityId = partialContactId, EntityType = EntityTypes.Person }
            ];

            List<Relationship> allRels = new List<Relationship>();
            allRels.AddRange(initialRelationships);
            allRels.AddRange(partialContactRelationships);

            // Mock ListAsync<Relationship> (2-arg and 3-arg)
             _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct) =>
                {
                    var func = predicate.Compile();
                    return allRels.Where(func).ToList();
                });

             _repositoryMock.Setup(r => r.ListAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<Relationship, bool>> predicate, CancellationToken ct, string[] includes) =>
                {
                    var func = predicate.Compile();
                    return allRels.Where(func).ToList();
                });

            // Mock Dependencies (empty)
            _repositoryMock.Setup(r => r.ListAsync<Pet>(It.IsAny<Expression<Func<Pet, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Fact>(It.IsAny<Expression<Func<Fact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _repositoryMock.Setup(r => r.ListAsync<Note>(It.IsAny<Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Contacts
            List<Contact> linkedContacts = [
                new Contact { Id = partialContactId, IsPartial = true }
            ];
            List<Contact> fullContacts = [
                 new Contact { Id = otherFullContactId, IsPartial = false }
            ];

            var contactMockFunc = new Func<Expression<Func<Contact, bool>>, CancellationToken, string[], List<Contact>>((predicate, ct, includes) =>
            {
                Func<Contact, bool> func = predicate.Compile();
                var allRelevantContacts = new List<Contact>();
                allRelevantContacts.AddRange(linkedContacts);
                allRelevantContacts.AddRange(fullContacts);
                return allRelevantContacts.Where(func).ToList();
            });

            _repositoryMock.Setup(r => r.ListAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contactMockFunc);

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contactMockFunc);

            // Act
            await _service.DeleteContactAsync(contactId);

            // Assert
            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(contactId, It.IsAny<CancellationToken>()), Times.Once); // Main contact deleted
            _repositoryMock.Verify(r => r.DeleteAsync<Contact>(partialContactId, It.IsAny<CancellationToken>()), Times.Never); // Partial contact kept
        }
    }
}
