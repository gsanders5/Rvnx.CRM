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
    public class ContactManagementServiceImageTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly ContactManagementService _service;
        private readonly List<Attachment> _attachments = [];
        private readonly List<ContactMethod> _contactMethods = [];
        private readonly List<SignificantDate> _significantDates = [];

        public ContactManagementServiceImageTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _service = new ContactManagementService(_repositoryMock.Object, _fileValidationServiceMock.Object);

            // Setup ListAsync for Attachment
            _repositoryMock.Setup(r => r.ListAsync<Attachment>(
                It.IsAny<Expression<Func<Attachment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Attachment, bool>> predicate, CancellationToken ct) =>
                {
                    return _attachments.AsQueryable().Where(predicate).ToList();
                });

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
        }

        [Fact]
        public async Task UpdateContactWithValidImageAddsAttachment()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(contactId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contact);

            // Mock File Validation
            _fileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            _fileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            ContactFormDto dto = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            string fileName = "test.jpg";
            string contentType = "image/jpeg";
            byte[] fileContent = [1, 2, 3];
            using MemoryStream stream = new(fileContent);

            // Act
            ContactOperationResult result = await _service.UpdateContactAsync(contactId, dto, stream, fileName, contentType);

            // Assert
            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Attachment>(a =>
                a.ContactId == contactId &&
                a.AttachmentType == AttachmentTypes.ProfileImage &&
                a.FileName == fileName &&
                a.ContentType == contentType),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
