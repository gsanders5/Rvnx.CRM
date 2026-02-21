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

namespace Rvnx.CRM.Tests.Controllers
{
    public class AttachmentsControllerIdorTests
    {
        private readonly Guid _currentUserId = Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938");

        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockCurrentUserService = new();
            mockCurrentUserService.Setup(s => s.UserId).Returns(_currentUserId);
            mockCurrentUserService.Setup(s => s.UserName).Returns("test-user");

            return new CRMDbContext(options, mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Upload_ShouldReturnNotFound_WhenEntityBelongsToAnotherUser()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repo = new(context);

            Mock<IFileValidationService> fileServiceMock = new();
            // Allow any file for this test
            fileServiceMock.Setup(s => s.IsImageExtension(It.IsAny<string>())).Returns(false);

            AttachmentsController controller = new(repo, fileServiceMock.Object, new EntityService(repo));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.Request.Headers["Referer"] = "http://localhost/Contacts";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(false);
            controller.Url = urlHelperMock.Object;

            // Create a contact belonging to another user
            Guid otherUserId = Guid.NewGuid();
            Contact otherUserContact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Target",
                LastName = "User",
                UserId = otherUserId
            };
            context.Contacts.Add(otherUserContact);
            context.SaveChanges();

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
            IActionResult result = await controller.Upload(otherUserContact.Id, "Person", fileMock.Object);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
