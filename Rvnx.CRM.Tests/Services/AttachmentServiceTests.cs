using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Tests.Helpers;

namespace Rvnx.CRM.Tests.Services;

public class AttachmentServiceTests
{
    private static CRMDbContext GetInMemoryDbContext() => TestDbContextFactory.CreateForDefaultUser();

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

        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        byte[] content = [1, 2, 3];

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, content, "test.png");

        Assert.True(result.Success);
        Assert.NotNull(result.AttachmentId);
        Attachment? attachment = await context.Attachments!.FindAsync(result.AttachmentId);
        Assert.NotNull(attachment);
        Assert.Equal("test.png", attachment.FileName);
    }

    [Fact]
    public async Task UploadAttachmentAsyncShouldFailWhenEntityNotFound()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        AttachmentOperationResult result = await service.UploadAttachmentAsync(Guid.NewGuid(), [1], "test.png");

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task UploadAttachmentAsyncShouldFailWhenPartialContact()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true });
        context.SaveChanges();

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, [1], "test.png");

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        Assert.Contains("partial contact", result.Errors[0]);
    }

    [Fact]
    public async Task UploadAttachmentAsyncShouldFailWhenFileEmpty()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);
        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, [], "test.png");

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

        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);
        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, [1, 2, 3], "test.png");

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

        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);
        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, [1, 2, 3], "test.exe");

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

        Mock<IContactLookupService> contactLookupServiceMock = new();
        contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);
        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
        context.SaveChanges();

        AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, [1, 2, 3], "test.png");

        Assert.False(result.Success);
        Assert.Contains("Invalid file signature", result.Errors[0]);
    }

    [Fact]
    public async Task DeleteAttachmentAsyncShouldSucceedWhenValid()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png"
        });
        context.SaveChanges();

        AttachmentOperationResult result = await service.DeleteAttachmentAsync(attachmentId);

        Assert.True(result.Success);
        Assert.Equal(attachmentId, result.AttachmentId);

        Attachment? deletedAttachment = await context.Attachments.FindAsync(attachmentId);
        Assert.Null(deletedAttachment);
    }

    [Fact]
    public async Task DeleteAttachmentAsyncShouldReturnNotFoundWhenAttachmentDoesNotExist()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        AttachmentOperationResult result = await service.DeleteAttachmentAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task DeleteAttachmentAsyncShouldReturnNotFoundWhenContactIsPartial()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true });

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png"
        });
        context.SaveChanges();

        AttachmentOperationResult result = await service.DeleteAttachmentAsync(attachmentId);

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        Assert.Contains("Cannot modify partial contact", result.Errors[0]);

        Attachment? attachment = await context.Attachments.FindAsync(attachmentId);
        Assert.NotNull(attachment);
    }

    [Fact]
    public async Task GetAttachmentAsyncShouldSucceedWhenValid()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png",
            AttachmentType = AttachmentTypes.General
        });
        context.SaveChanges();

        AttachmentDto? result = await service.GetAttachmentAsync(attachmentId);

        Assert.NotNull(result);
        Assert.Equal(attachmentId, result.Id);
        Assert.Equal("test.png", result.FileName);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(AttachmentTypes.General, result.AttachmentType);
        Assert.Equal(contactId, result.ContactId);
    }

    [Fact]
    public async Task GetAttachmentAsyncShouldReturnNullWhenAttachmentDoesNotExist()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        AttachmentDto? result = await service.GetAttachmentAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachmentAsyncShouldReturnNullWhenContactIsPartial()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true });

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png"
        });
        context.SaveChanges();

        AttachmentDto? result = await service.GetAttachmentAsync(attachmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachmentContentAsyncShouldSucceedWhenValid()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });

        Guid attachmentId = Guid.NewGuid();
        byte[] fileContent = [1, 2, 3];
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png",
            AttachmentContent = new AttachmentContent { Content = fileContent },
            LastChangedDate = new DateTime(2023, 1, 1)
        });
        context.SaveChanges();

        AttachmentContentDto? result = await service.GetAttachmentContentAsync(attachmentId);

        Assert.NotNull(result);
        Assert.Equal(attachmentId, result.Id);
        Assert.Equal(fileContent, result.Content);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("test.png", result.FileName);
        // Ignore LastChangedDate as it is overwritten by the framework on SaveChanges
    }

    [Fact]
    public async Task GetAttachmentContentAsyncShouldReturnNullWhenAttachmentDoesNotExist()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        AttachmentContentDto? result = await service.GetAttachmentContentAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachmentContentAsyncShouldReturnNullWhenContactIsPartial()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Partial", IsPartial = true });

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            ContactId = contactId,
            FileName = "test.png",
            ContentType = "image/png",
            AttachmentContent = new AttachmentContent { Content = [1, 2, 3] }
        });
        context.SaveChanges();

        AttachmentContentDto? result = await service.GetAttachmentContentAsync(attachmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAttachmentContentAsyncShouldHandleAttachmentWithoutContent()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid attachmentId = Guid.NewGuid();
        context.Attachments!.Add(new Attachment
        {
            Id = attachmentId,
            FileName = "test.png",
            ContentType = "image/png"
        });
        context.SaveChanges();

        AttachmentContentDto? result = await service.GetAttachmentContentAsync(attachmentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByContactAsyncShouldReturnAllAttachmentsForContact()
    {
        using CRMDbContext context = GetInMemoryDbContext();
        Repository repo = new(context);
        Mock<IFileValidationService> fileServiceMock = new();
        Mock<IContactLookupService> contactLookupServiceMock = new();

        AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

        Guid contactId = Guid.NewGuid();
        context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });

        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        Guid id3 = Guid.NewGuid();
        context.Attachments!.AddRange(
            new Attachment { Id = id1, ContactId = contactId, FileName = "file1.pdf", ContentType = "application/pdf", AttachmentType = AttachmentTypes.General },
            new Attachment { Id = id2, ContactId = contactId, FileName = "file2.png", ContentType = "image/png", AttachmentType = AttachmentTypes.General },
            new Attachment { Id = id3, ContactId = contactId, FileName = "file3.txt", ContentType = "text/plain", AttachmentType = AttachmentTypes.General }
        );
        context.SaveChanges();

        List<AttachmentDto> result = await service.GetByContactAsync(contactId);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.Id == id1 && a.FileName == "file1.pdf" && a.ContentType == "application/pdf");
        Assert.Contains(result, a => a.Id == id2 && a.FileName == "file2.png" && a.ContentType == "image/png");
        Assert.Contains(result, a => a.Id == id3 && a.FileName == "file3.txt" && a.ContentType == "text/plain");
    }

    public class AttachmentContentTypeSecurityTests
    {
        private static CRMDbContext GetInMemoryDbContext() => TestDbContextFactory.CreateForDefaultUser();

        [Fact]
        public async Task UploadAttachmentShouldIgnoreMaliciousContentTypeAndUseSafeMimeType()
        {
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            Mock<IFileValidationService> fileServiceMock = new();

            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.GetMimeType(It.IsAny<string>())).Returns("text/plain");

            Mock<IContactLookupService> contactLookupServiceMock = new();
            contactLookupServiceMock.Setup(s => s.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, contactLookupServiceMock.Object);

            Guid contactId = Guid.NewGuid();
            context.Contacts!.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            byte[] content = [1, 2, 3]; // Dummy content
            string fileName = "innocent.txt";

            // The malicious Content-Type is no longer even passed to the method.
            // The method signature change itself is part of the security fix (Defense in Depth / Trust Nothing).
            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, content, fileName);

            Assert.True(result.Success);

            Attachment? attachment = await context.Attachments!.FindAsync(result.AttachmentId);
            Assert.NotNull(attachment);

            // SECURITY CHECK: The ContentType should be "text/plain", NOT "text/html"
            Assert.Equal("text/plain", attachment.ContentType);
        }
    }
}
