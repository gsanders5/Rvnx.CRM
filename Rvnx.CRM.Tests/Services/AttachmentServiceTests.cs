using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;

namespace Rvnx.CRM.Tests.Services
{
    public class AttachmentServiceTests
    {
        private static CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938")); // Test User ID
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldSucceedWhenValid()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.GetMimeType(It.IsAny<string>())).Returns("image/png");

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            byte[] content = [1, 2, 3];

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, content, "test.png");

            Assert.True(result.Success);
            Assert.NotNull(result.AttachmentId);
            Attachment? attachment = await context.Attachments.FindAsync(result.AttachmentId);
            Assert.NotNull(attachment);
            Assert.Equal("test.png", attachment.FileName);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenEntityNotFound()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);

            AttachmentOperationResult result = await service.UploadAttachmentAsync(Guid.NewGuid(), EntityTypes.Person, [1], "test.png");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenPartialContact()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true });
            context.SaveChanges();

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, [1], "test.png");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("partial contact", result.Errors[0]);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenEntityTypeNotSupported()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            Mock<IEntityService> entityServiceMock = new();
            // No need to mock EntityService for this check as it happens before existence check

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);

            AttachmentOperationResult result = await service.UploadAttachmentAsync(Guid.NewGuid(), "UnsupportedType", [1, 2, 3], "test.txt");

            Assert.False(result.Success);
            Assert.Contains("not currently supported", result.Errors[0]);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenFileEmpty()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, [], "test.png");

            Assert.False(result.Success);
            Assert.Contains("File is empty", result.Errors[0]);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenFileSizeTooLarge()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(false);

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, [1, 2, 3], "test.png");

            Assert.False(result.Success);
            Assert.Contains("File is too large", result.Errors[0]);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenExtensionNotAllowed()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(false);

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, [1, 2, 3], "test.exe");

            Assert.False(result.Success);
            Assert.Contains("File type not allowed", result.Errors[0]);
        }

        [Fact]
        public async Task UploadAttachmentAsyncShouldFailWhenSignatureInvalid()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);
            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, [1, 2, 3], "test.png");

            Assert.False(result.Success);
            Assert.Contains("Invalid file signature", result.Errors[0]);
        }
    }
}
