using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using System.Text;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerTests
    {
        [Fact]
        public async Task ViewShouldReturnFileContentResultWhenImageExists()
        {
            // Arrange
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

            AttachmentsController controller = new(serviceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal(content, fileResult.FileContents);
        }

        [Fact]
        public async Task ViewShouldReturnNotFoundWhenAttachmentDoesNotExist()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            serviceMock.Setup(s => s.GetAttachmentContentAsync(It.IsAny<Guid>()))
                .ReturnsAsync((AttachmentContentDto?)null);

            AttachmentsController controller = new(serviceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            IActionResult result = await controller.View(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ViewShouldReturn304WhenIfModifiedSinceIsCurrent()
        {
            // Arrange
            Mock<IAttachmentService> serviceMock = new();
            Guid attachmentId = Guid.NewGuid();
            DateTime lastChanged = DateTime.UtcNow.AddMinutes(-10); // Fixed time

            serviceMock.Setup(s => s.GetAttachmentContentAsync(attachmentId))
                .ReturnsAsync(new AttachmentContentDto
                {
                    Id = attachmentId,
                    Content = [1, 2, 3],
                    ContentType = "image/png",
                    FileName = "test.png",
                    LastChangedDate = lastChanged
                });

            AttachmentsController controller = new(serviceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Truncate milliseconds/ticks to match HTTP header precision
            DateTime headerDate = lastChanged.AddTicks(-(lastChanged.Ticks % TimeSpan.TicksPerSecond));
            controller.Request.Headers["If-Modified-Since"] = headerDate.ToString("R");

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(304, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ViewShouldReturnFileWhenIfModifiedSinceIsOld()
        {
            // Arrange
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

            AttachmentsController controller = new(serviceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Header is 10 minutes in the past
            controller.Request.Headers["If-Modified-Since"] = lastChanged.AddMinutes(-10).ToString("R");

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            Assert.IsType<FileContentResult>(result);
            Assert.True(controller.Response.Headers.ContainsKey("Last-Modified"));
            Assert.True(controller.Response.Headers.ContainsKey("Cache-Control"));
        }
    }
}
