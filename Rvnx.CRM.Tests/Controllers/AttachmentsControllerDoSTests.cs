using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
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

            AttachmentsController controller = new(attachmentServiceMock.Object, fileValidationServiceMock.Object);

            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.FileName).Returns("large.pdf");
            fileMock.Setup(f => f.Length).Returns(50 * 1024 * 1024); // 50MB
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable(); // We want to verify this is NOT called

            IActionResult result = await controller.Upload(Guid.NewGuid(), "Person", fileMock.Object);

            Assert.IsType<BadRequestObjectResult>(result);

            // 2. Crucially, CopyToAsync should NEVER be called to prevent memory exhaustion
            fileMock.Verify(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never,
                "Stream should not be read if file size exceeds limit");
        }
    }
}
