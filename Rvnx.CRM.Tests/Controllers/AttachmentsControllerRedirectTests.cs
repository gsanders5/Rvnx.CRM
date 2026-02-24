using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerRedirectTests
    {
        private static AttachmentsController GetController(Mock<IAttachmentService> serviceMock)
        {
            Mock<IFileValidationService> fileValidationMock = new();
            fileValidationMock.Setup(f => f.IsAllowedExtension(It.IsAny<string>())).Returns(true);

            AttachmentsController controller = new(serviceMock.Object, fileValidationMock.Object)
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
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
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.DeleteAttachmentAsync(It.IsAny<Guid>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            string returnUrl = "/Contacts/Details/123";

            // Act
            IActionResult result = await controller.Delete(Guid.NewGuid(), returnUrl);

            // Assert
            LocalRedirectResult redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task DeleteShouldRedirectToHomeWhenReturnUrlInvalidAndRefererUnsafe()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.DeleteAttachmentAsync(It.IsAny<Guid>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            string unsafeReferer = "http://evil.com/exploit";
            controller.Request.Headers["Referer"] = unsafeReferer;

            // Act
            IActionResult result = await controller.Delete(Guid.NewGuid(), null);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
    }
}
