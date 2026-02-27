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

namespace Rvnx.CRM.Tests.Security
{
    public class AttachmentContentTypeSecurityTests
    {
        private static CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

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

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentService service = new(repo, fileServiceMock.Object, entityServiceMock.Object);

            Guid contactId = Guid.NewGuid();
            context.Contacts.Add(new Contact { Id = contactId, FirstName = "Test", LastName = "User" });
            context.SaveChanges();

            byte[] content = [1, 2, 3]; // Dummy content
            string fileName = "innocent.txt";

            // The malicious Content-Type is no longer even passed to the method.
            // The method signature change itself is part of the security fix (Defense in Depth / Trust Nothing).
            AttachmentOperationResult result = await service.UploadAttachmentAsync(contactId, EntityTypes.Person, content, fileName);

            Assert.True(result.Success);

            Attachment? attachment = await context.Attachments.FindAsync(result.AttachmentId);
            Assert.NotNull(attachment);

            // SECURITY CHECK: The ContentType should be "text/plain", NOT "text/html"
            Assert.Equal("text/plain", attachment.ContentType);
        }
    }
}
