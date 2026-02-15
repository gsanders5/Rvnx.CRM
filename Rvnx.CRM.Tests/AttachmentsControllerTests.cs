using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using System.Text;
using Xunit;

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
            using var context = GetInMemoryDbContext();
            var repo = new Repository(context);
            var controller = new AttachmentsController(repo);

            var attachmentId = Guid.NewGuid();
            var content = Encoding.UTF8.GetBytes("fake image content");
            var contentType = "image/png";

            var attachment = new Attachment
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
            var result = await controller.View(attachmentId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.Equal(content, fileResult.FileContents);
        }

        [Fact]
        public async Task View_ShouldReturnNotFound_WhenAttachmentDoesNotExist()
        {
             // Arrange
            using var context = GetInMemoryDbContext();
            var repo = new Repository(context);
            var controller = new AttachmentsController(repo);

            // Act
            var result = await controller.View(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
