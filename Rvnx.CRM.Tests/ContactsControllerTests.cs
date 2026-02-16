using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerTests
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
        public async Task Edit_Post_ShouldReturnValidationError_WhenFileIsNotImage()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            ContactsController controller = new(repository, loggerMock.Object);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            UpdateContactDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            Mock<IFormFile> fileMock = new();
            string content = "This is not an image";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            IActionResult result = await controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            // Assert that the error is added to the model-level errors (empty key)
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_ShouldSaveAttachment_WhenFileIsImage()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            ContactsController controller = new(repository, loggerMock.Object);

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            UpdateContactDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            Mock<IFormFile> fileMock = new();
            string content = "Fake Image Content";
            MemoryStream ms = new();
            StreamWriter writer = new(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("image.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            List<Attachment> attachments = await repository.ListAsync<Attachment>(a => a.EntityId == contactId && a.AttachmentType == "ProfileImage");
            Assert.Single(attachments);
            Assert.Equal("image.png", attachments[0].FileName);
        }
    }
}
