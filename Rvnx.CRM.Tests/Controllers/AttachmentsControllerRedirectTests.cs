using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerRedirectTests
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

        private static AttachmentsController GetController(CRMDbContext context)
        {
            Mock<IRepository> repoMock = new();
            repoMock.Setup(r => r.ExistsAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            repoMock.Setup(r => r.AddAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Attachment());
            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsImageExtension(It.IsAny<string>())).Returns(false);
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            Mock<IEntityService> entityServiceMock = new();
            entityServiceMock.Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

            AttachmentsController controller = new(repoMock.Object, fileServiceMock.Object, entityServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Set Request Host to localhost
            controller.Request.Host = new HostString("localhost");

            Mock<IUrlHelper> urlHelperMock = new();
            // IsLocalUrl logic: starts with /
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) => !string.IsNullOrEmpty(url) && url.StartsWith('/'));

            controller.Url = urlHelperMock.Object;

            return controller;
        }

        private static IFormFile CreateMockFile()
        {
            Mock<IFormFile> fileMock = new();
            MemoryStream ms = new(new byte[] { 1, 2, 3 });
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) => ms.CopyTo(stream))
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");
            return fileMock.Object;
        }

        [Fact]
        public async Task UploadShouldRedirectToReturnUrlWhenValidLocalUrl()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            string returnUrl = "/Contacts/Details/123";
            IFormFile file = CreateMockFile();

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file, returnUrl);

            // Assert
            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task UploadShouldRedirectToRefererWhenReturnUrlMissingAndRefererIsSafe()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            IFormFile file = CreateMockFile();

            string safeReferer = "http://localhost/Contacts/Details/123";
            controller.Request.Headers["Referer"] = safeReferer;

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file, null);

            // Assert
            RedirectResult redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(safeReferer, redirectResult.Url);
        }

        [Fact]
        public async Task UploadShouldRedirectToHomeWhenReturnUrlMissingAndRefererIsUnsafe()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            IFormFile file = CreateMockFile();

            string unsafeReferer = "http://evil.com/exploit";
            controller.Request.Headers["Referer"] = unsafeReferer;

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file, null);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task UploadShouldRedirectToHomeWhenReturnUrlAndRefererAreMissing()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            IFormFile file = CreateMockFile();

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file, null);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task DeleteShouldRedirectToReturnUrlWhenValidLocalUrl()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            string returnUrl = "/Contacts/Details/123";

            Guid attachmentId = Guid.NewGuid();
            context.Attachments.Add(new Attachment { Id = attachmentId, ContactId = Guid.NewGuid(), ContentType = "text/plain", AttachmentType = "General" });
            context.SaveChanges();

            // Act
            IActionResult result = await controller.Delete(attachmentId, returnUrl);

            // Assert
            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task DeleteShouldRedirectToHomeWhenReturnUrlInvalidAndRefererUnsafe()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            AttachmentsController controller = GetController(context);
            string unsafeReferer = "http://evil.com/exploit";
            controller.Request.Headers["Referer"] = unsafeReferer;

            Guid attachmentId = Guid.NewGuid();
            context.Attachments.Add(new Attachment { Id = attachmentId, ContactId = Guid.NewGuid(), ContentType = "text/plain", AttachmentType = "General" });
            context.SaveChanges();

            // Act
            IActionResult result = await controller.Delete(attachmentId, null);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
    }
}
