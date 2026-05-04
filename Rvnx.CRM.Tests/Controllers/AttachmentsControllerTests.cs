using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using System.Text;

namespace Rvnx.CRM.Tests.Controllers;

public class AttachmentsControllerTests
{
    public class AttachmentsControllerDoSTests
    {

        [Fact]
        public async Task UploadShouldNotReadStreamWhenFileExceedsSizeLimit()
        {
            Mock<IAttachmentService> attachmentServiceMock = new();
            Mock<IFileValidationService> fileValidationServiceMock = new();

            fileValidationServiceMock.Setup(x => x.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileValidationServiceMock.Setup(x => x.IsAllowedFileSize(It.IsAny<long>())).Returns(false);

            AttachmentsController controller = new(
                attachmentServiceMock.Object,
                fileValidationServiceMock.Object,
                new Mock<IThumbnailService>().Object);

            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.FileName).Returns("large.pdf");
            fileMock.Setup(f => f.Length).Returns(50 * 1024 * 1024); // 50MB
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable(); // We want to verify this is NOT called

            IActionResult result = await controller.Upload(Guid.NewGuid(), fileMock.Object);

            Assert.IsType<BadRequestObjectResult>(result);

            // 2. Crucially, CopyToAsync should NEVER be called to prevent memory exhaustion
            fileMock.Verify(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never,
                "Stream should not be read if file size exceeds limit");
        }

    }
    public class AttachmentsControllerIdorTests
    {

        [Fact]
        public async Task UploadShouldReturnNotFoundWhenEntityBelongsToAnotherUser()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.NotFound("Entity not found."));

            Mock<IFileValidationService> fileValidationMock = new();
            fileValidationMock.Setup(f => f.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileValidationMock.Setup(f => f.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            AttachmentsController controller = new(
                serviceMock.Object,
                fileValidationMock.Object,
                new Mock<IThumbnailService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(false);
            controller.Url = urlHelperMock.Object;

            Mock<IFormFile> fileMock = new();
            MemoryStream ms = new(new byte[] { 1, 2, 3 });
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) => ms.CopyTo(stream))
                .Returns(Task.CompletedTask);

            IActionResult result = await controller.Upload(Guid.NewGuid(), fileMock.Object);

            Assert.IsType<NotFoundObjectResult>(result);
        }

    }
    public class AttachmentsControllerRedirectTests
    {

        private static AttachmentsController GetController(Mock<IAttachmentService> serviceMock)
        {
            Mock<IFileValidationService> fileValidationMock = new();
            fileValidationMock.Setup(f => f.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileValidationMock.Setup(f => f.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            AttachmentsController controller = new(
                serviceMock.Object,
                fileValidationMock.Object,
                new Mock<IThumbnailService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            controller.Request.Host = new HostString("localhost");

            Mock<IUrlHelper> urlHelperMock = new();
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            string returnUrl = "/Contacts/Details/123";
            IFormFile file = CreateMockFile();

            IActionResult result = await controller.Upload(Guid.NewGuid(), file, returnUrl);

            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task UploadShouldRedirectToRefererWhenReturnUrlMissingAndRefererIsSafe()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            IFormFile file = CreateMockFile();

            string safeReferer = "http://localhost/Contacts/Details/123";
            controller.Request.Headers["Referer"] = safeReferer;

            IActionResult result = await controller.Upload(Guid.NewGuid(), file, null);

            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/Contacts/Details/123", redirectResult.Url);
        }

        [Fact]
        public async Task UploadShouldRedirectToHomeWhenReturnUrlMissingAndRefererIsUnsafe()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            IFormFile file = CreateMockFile();

            string unsafeReferer = "http://evil.com/exploit";
            controller.Request.Headers["Referer"] = unsafeReferer;

            IActionResult result = await controller.Upload(Guid.NewGuid(), file, null);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task UploadShouldRedirectToHomeWhenReturnUrlAndRefererAreMissing()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            IFormFile file = CreateMockFile();

            IActionResult result = await controller.Upload(Guid.NewGuid(), file, null);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task DeleteShouldRedirectToReturnUrlWhenValidLocalUrl()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.DeleteAttachmentAsync(It.IsAny<Guid>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            string returnUrl = "/Contacts/Details/123";

            IActionResult result = await controller.Delete(Guid.NewGuid(), returnUrl);

            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task DeleteShouldRedirectToHomeWhenReturnUrlInvalidAndRefererUnsafe()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.DeleteAttachmentAsync(It.IsAny<Guid>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            string unsafeReferer = "http://evil.com/exploit";
            controller.Request.Headers["Referer"] = unsafeReferer;

            IActionResult result = await controller.Delete(Guid.NewGuid(), null);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

    }
    public class AttachmentsControllerSecurityTests
    {

        private static AttachmentsController GetController(Mock<IAttachmentService> serviceMock)
        {
            Mock<IFileValidationService> fileValidationMock = new();
            fileValidationMock.Setup(f => f.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileValidationMock.Setup(f => f.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            AttachmentsController controller = new(
                serviceMock.Object,
                fileValidationMock.Object,
                new Mock<IThumbnailService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(false);
            controller.Url = urlHelperMock.Object;
            controller.Request.Host = new HostString("localhost");
            return controller;
        }

        private static IFormFile CreateMockFile(string filename, string contentType, long length)
        {
            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns(filename);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            return fileMock.Object;
        }

        [Fact]
        public async Task UploadShouldRejectWhenExtensionIsImageButContentIsNot()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("Invalid file signature."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("exploit.png", "image/png", 100);

            IActionResult result = await controller.Upload(Guid.NewGuid(), file);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }

        [Fact]
        public async Task UploadShouldSucceedWhenExtensionIsImageAndContentIsImage()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("valid.png", "image/png", 100);

            IActionResult result = await controller.Upload(Guid.NewGuid(), file);

            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/Contacts", redirectResult.Url);
        }

        [Fact]
        public async Task UploadShouldRejectWhenExtensionIsPdfButContentIsNot()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("Invalid file signature."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("fake.pdf", "application/pdf", 100);

            IActionResult result = await controller.Upload(Guid.NewGuid(), file);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }

        [Fact]
        public async Task UploadShouldRejectWhenFileExceedsSizeLimit()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("File is too large."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            long fileSize = 31 * 1024 * 1024;
            IFormFile file = CreateMockFile("largefile.pdf", "application/pdf", fileSize);

            IActionResult result = await controller.Upload(Guid.NewGuid(), file);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is too large.", badRequest.Value);
        }

    }
    public class General
    {

        private static AttachmentsController GetController(Mock<IAttachmentService> serviceMock)
        {
            return new AttachmentsController(
                serviceMock.Object,
                new Mock<IFileValidationService>().Object,
                new Mock<IThumbnailService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task ViewShouldReturnFileContentResultWhenImageExists()
        {
            Mock<IAttachmentService> serviceMock = new();
            Guid attachmentId = Guid.NewGuid();
            byte[] content = Encoding.UTF8.GetBytes("fake image content");
            string contentType = "image/png";

            serviceMock.Setup(s => s.GetAttachmentContentAsync(attachmentId))
                .ReturnsAsync(new AttachmentContentDto
                {
                    Id = attachmentId,
                    Content = content,
                    ContentType = contentType,
                    FileName = "test.png",
                    LastChangedDate = DateTime.UtcNow
                });

            AttachmentsController controller = GetController(serviceMock);

            IActionResult result = await controller.View(attachmentId);

            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal(content, fileResult.FileContents);
        }

        [Fact]
        public async Task ViewShouldReturnNotFoundWhenAttachmentDoesNotExist()
        {
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.GetAttachmentContentAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AttachmentContentDto?)null);

            AttachmentsController controller = GetController(serviceMock);

            IActionResult result = await controller.View(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ViewShouldReturn304WhenIfModifiedSinceIsCurrent()
        {
            Mock<IAttachmentService> serviceMock = new();
            Guid attachmentId = Guid.NewGuid();
            DateTime lastChanged = DateTime.UtcNow.AddMinutes(-10);

            serviceMock.Setup(s => s.GetAttachmentContentAsync(attachmentId))
                .ReturnsAsync(new AttachmentContentDto
                {
                    Id = attachmentId,
                    Content = [1, 2, 3],
                    ContentType = "image/png",
                    FileName = "test.png",
                    LastChangedDate = lastChanged
                });

            AttachmentsController controller = GetController(serviceMock);

            // Truncate milliseconds/ticks to match HTTP header precision
            DateTime headerDate = lastChanged.AddTicks(-(lastChanged.Ticks % TimeSpan.TicksPerSecond));
            controller.Request.Headers["If-Modified-Since"] = headerDate.ToString("R");

            IActionResult result = await controller.View(attachmentId);

            StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(304, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ViewShouldReturnFileWhenIfModifiedSinceIsOld()
        {
            Mock<IAttachmentService> serviceMock = new();
            Guid attachmentId = Guid.NewGuid();
            DateTime lastChanged = DateTime.UtcNow;

            serviceMock.Setup(s => s.GetAttachmentContentAsync(attachmentId))
                .ReturnsAsync(new AttachmentContentDto
                {
                    Id = attachmentId,
                    Content = [1, 2, 3],
                    ContentType = "image/png",
                    FileName = "test.png",
                    LastChangedDate = lastChanged
                });

            AttachmentsController controller = GetController(serviceMock);

            // Header is 10 minutes in the past
            controller.Request.Headers["If-Modified-Since"] = lastChanged.AddMinutes(-10).ToString("R");

            IActionResult result = await controller.View(attachmentId);

            Assert.IsType<FileContentResult>(result);
            Assert.True(controller.Response.Headers.ContainsKey("Last-Modified"));
            Assert.True(controller.Response.Headers.ContainsKey("Cache-Control"));
        }

    }
}
