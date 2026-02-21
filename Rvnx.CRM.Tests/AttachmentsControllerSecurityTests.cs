using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
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
            mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
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
            // IsValidFileSignature returns false -> Fail
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            AttachmentsController controller = new(repo, fileServiceMock.Object, new EntityService(repo));
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

            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
            context.Contacts.Add(contact);
            context.SaveChanges();

            // Act
            IActionResult result = await controller.Upload(contact.Id, "Person", fileMock.Object);

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
            // IsValidFileSignature returns true -> Success
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            AttachmentsController controller = new(repo, fileServiceMock.Object, new EntityService(repo));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";
            controller.Request.Host = new HostString("localhost");

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(false);
            controller.Url = urlHelperMock.Object;

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

            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
            context.Contacts.Add(contact);
            context.SaveChanges();

            // Act
            IActionResult result = await controller.Upload(contact.Id, "Person", fileMock.Object);

            // Assert
            Assert.IsType<RedirectResult>(result);

            // Verify it was added to DB
            Assert.Single(context.Attachments);
        }

        [Fact]
        public async Task Upload_ShouldReject_WhenExtensionIsPdfButContentIsNot()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            Mock<IFileValidationService> fileServiceMock = new();
            // IsValidFileSignature returns false for bad PDF
            fileServiceMock.Setup(s => s.IsValidFileSignature(It.IsAny<byte[]>(), ".pdf")).Returns(false);

            AttachmentsController controller = new(repo, fileServiceMock.Object, new EntityService(repo));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            Mock<IFormFile> fileMock = new();
            string content = "Not a PDF";
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
            fileMock.Setup(f => f.FileName).Returns("fake.pdf");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("application/pdf");

            Contact contact = new() { Id = Guid.NewGuid(), FirstName = "Test", LastName = "User" };
            context.Contacts.Add(contact);
            context.SaveChanges();

            // Act
            IActionResult result = await controller.Upload(contact.Id, "Person", fileMock.Object);

            // Assert
            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid file signature.", badRequest.Value);
        }
    }
}
