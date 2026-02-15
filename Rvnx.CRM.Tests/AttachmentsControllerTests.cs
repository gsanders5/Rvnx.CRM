using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            CRMDbContext context = new(options);
            return context;
        }

        [Fact]
        public async Task View_ShouldReturnFileContentResult_WhenImageExists()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);
            AttachmentsController controller = new(repo);

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

            // Act
            IActionResult result = await controller.View(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
