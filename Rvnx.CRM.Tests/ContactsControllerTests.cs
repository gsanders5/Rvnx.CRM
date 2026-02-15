using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new CRMDbContext(options);
        }

        [Fact]
        public async Task Edit_Post_ShouldReturnValidationError_WhenFileIsNotImage()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository(context);
            var loggerMock = new Mock<ILogger<ContactsController>>();
            var controller = new ContactsController(repository, loggerMock.Object);

            var contactId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            var dto = new UpdateContactDto { Id = contactId, FirstName = "John", LastName = "Doe" };

            var fileMock = new Mock<IFormFile>();
            var content = "This is not an image";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("test.txt");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            var result = await controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            // Assert that the error is added to the model-level errors (empty key)
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_ShouldSaveAttachment_WhenFileIsImage()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var repository = new Repository(context);
            var loggerMock = new Mock<ILogger<ContactsController>>();
            var controller = new ContactsController(repository, loggerMock.Object);

            var contactId = Guid.NewGuid();
            var contact = new Contact { Id = contactId, FirstName = "John", LastName = "Doe" };
            await repository.AddAsync(contact);
            await repository.SaveChangesAsync();

            var dto = new UpdateContactDto { Id = contactId, FirstName = "John", LastName = "Doe" };

            var fileMock = new Mock<IFormFile>();
            var content = "Fake Image Content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
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
            var result = await controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var attachments = await repository.ListAsync<Attachment>(a => a.EntityId == contactId && a.AttachmentType == "ProfileImage");
            Assert.Single(attachments);
            Assert.Equal("image.png", attachments[0].FileName);
        }
    }
}
