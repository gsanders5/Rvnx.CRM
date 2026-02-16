using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using System.Text;

namespace Rvnx.CRM.Tests
{
    public class AttachmentsControllerTests
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
        public async Task View_ShouldReturnFileContentResult_WhenImageExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            AttachmentsController controller = new(repo);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            Guid attachmentId = Guid.NewGuid();
            byte[] content = Encoding.UTF8.GetBytes("fake image content");
            string contentType = "image/png";

            Attachment attachment = new()
            {
                Id = attachmentId,
                FileName = "test.png",
                ContentType = contentType,
                AttachmentContent = new AttachmentContent
                {
                    AttachmentId = attachmentId,
                    Content = content
                }
            };

            await repo.AddAsync(attachment);
            await repo.SaveChangesAsync();

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            FileContentResult fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal(content, fileResult.FileContents);
        }

        [Fact]
        public async Task View_ShouldReturnNotFound_WhenAttachmentDoesNotExist()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            AttachmentsController controller = new(repo);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            IActionResult result = await controller.View(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task View_ShouldReturn304_WhenIfModifiedSinceIsCurrent()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            AttachmentsController controller = new(repo);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            Guid attachmentId = Guid.NewGuid();
            Attachment attachment = new()
            {
                Id = attachmentId,
                FileName = "test.png",
                ContentType = "image/png",
                AttachmentContent = new AttachmentContent
                {
                    AttachmentId = attachmentId,
                    Content = [1, 2, 3]
                }
            };

            // Set LastChangedDate explicitly for consistent testing
            DateTime now = DateTime.UtcNow;
            // Floor to seconds as DB often does (or logic expects)
            now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
            attachment.LastChangedDate = now;

            await repo.AddAsync(attachment);
            await repo.SaveChangesAsync();

            // Set If-Modified-Since header to current LastChangedDate
            controller.Request.Headers["If-Modified-Since"] = now.ToString("R");

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            StatusCodeResult statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(304, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task View_ShouldReturnFile_WhenIfModifiedSinceIsOld()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            AttachmentsController controller = new(repo);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            Guid attachmentId = Guid.NewGuid();
            Attachment attachment = new()
            {
                Id = attachmentId,
                FileName = "test.png",
                ContentType = "image/png",
                AttachmentContent = new AttachmentContent
                {
                    AttachmentId = attachmentId,
                    Content = [1, 2, 3]
                }
            };

            DateTime now = DateTime.UtcNow;
            attachment.LastChangedDate = now;

            await repo.AddAsync(attachment);
            await repo.SaveChangesAsync();

            // Set If-Modified-Since header to older date
            controller.Request.Headers["If-Modified-Since"] = now.AddMinutes(-10).ToString("R");

            // Act
            IActionResult result = await controller.View(attachmentId);

            // Assert
            Assert.IsType<FileContentResult>(result);
            Assert.True(controller.Response.Headers.ContainsKey("Last-Modified"));
            Assert.True(controller.Response.Headers.ContainsKey("Cache-Control"));
        }
    }
}
