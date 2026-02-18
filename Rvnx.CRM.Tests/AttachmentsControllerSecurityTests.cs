using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class AttachmentsControllerSecurityTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns("test-user-id");
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Upload_ShouldReject_WhenExtensionIsImageButContentIsNot()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsImageExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            AttachmentsController controller = new(repo, fileServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            // Mock Referer header
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            Mock<IFormFile> fileMock = new();
            string content = "<html><script>alert(1)</script></html>";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                   ms.Position = 0;
                   ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("exploit.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", fileMock.Object);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }

        [Fact]
        public async Task Upload_ShouldSucceed_WhenExtensionIsImageAndContentIsImage()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            Mock<IFileValidationService> fileServiceMock = new();
            fileServiceMock.Setup(s => s.IsImageExtension(It.IsAny<string>())).Returns(true);
            fileServiceMock.Setup(s => s.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            AttachmentsController controller = new(repo, fileServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            Mock<IFormFile> fileMock = new();
            // PNG signature
            byte[] content = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 1, 2, 3 };
            MemoryStream ms = new(content);

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                   ms.Position = 0;
                   ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("valid.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", fileMock.Object);

            // Assert
            Assert.IsType<RedirectResult>(result);

            // Verify it was added to DB
            Assert.Single(context.Attachments);
        }
    }
}
