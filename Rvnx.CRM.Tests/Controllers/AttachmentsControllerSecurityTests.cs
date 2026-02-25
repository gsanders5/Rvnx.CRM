using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerSecurityTests
    {
        private static AttachmentsController GetController(Mock<IAttachmentService> serviceMock)
        {
            Mock<IFileValidationService> fileValidationMock = new();
            fileValidationMock.Setup(f => f.IsAllowedExtension(It.IsAny<string>())).Returns(true);
            fileValidationMock.Setup(f => f.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            AttachmentsController controller = new(serviceMock.Object, fileValidationMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            // Mock UrlHelper
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
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("Invalid file signature."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("exploit.png", "image/png", 100);

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }

        [Fact]
        public async Task UploadShouldSucceedWhenExtensionIsImageAndContentIsImage()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("valid.png", "image/png", 100);

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file);

            // Assert
            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task UploadShouldRejectWhenExtensionIsPdfButContentIsNot()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("Invalid file signature."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            IFormFile file = CreateMockFile("fake.pdf", "application/pdf", 100);

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }

        [Fact]
        public async Task UploadShouldRejectWhenFileExceedsSizeLimit()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.Failure("File is too large."));

            AttachmentsController controller = GetController(serviceMock);
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            long fileSize = 31 * 1024 * 1024;
            IFormFile file = CreateMockFile("largefile.pdf", "application/pdf", fileSize);

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", file);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is too large.", badRequest.Value);
        }
    }
}
