using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerIdorTests
    {
        [Fact]
        public async Task UploadShouldReturnNotFoundWhenEntityBelongsToAnotherUser()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(AttachmentOperationResult.NotFound("Entity not found."));

            Mock<IFileValidationService> validationMock = new();
            validationMock.Setup(v => v.IsAllowedExtension(It.IsAny<string>())).Returns(true);

            AttachmentsController controller = new(serviceMock.Object, validationMock.Object)
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

            // Act
            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", fileMock.Object);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
