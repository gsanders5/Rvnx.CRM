using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
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
        public async Task Index_ReturnsViewWithContacts()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            context.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            context.Contacts.Add(new Contact { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            List<ContactDto> model = Assert.IsAssignableFrom<List<ContactDto>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_Post_ShouldCreateContactAndRelatedEntities()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            CreateContactDto dto = new()
            {
                FirstName = "New",
                LastName = "User",
                Email = "new@user.com",
                Phone = "1234567890",
                Birthday = new DateTime(1990, 1, 1)
            };

            // Act
            IActionResult result = await controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Contact? contact = await context.Contacts.FirstOrDefaultAsync(c => c.FirstName == "New");
            Assert.NotNull(contact);

            // Check Email
            ContactMethod? email = await context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.EntityId == contact.Id && c.Type == ContactMethodType.Email);
            Assert.NotNull(email);
            Assert.Equal("new@user.com", email.Value);

            // Check Phone
            ContactMethod? phone = await context.Set<ContactMethod>().FirstOrDefaultAsync(c => c.EntityId == contact.Id && c.Type == ContactMethodType.Phone);
            Assert.NotNull(phone);
            Assert.Equal("1234567890", phone.Value);

            // Check Birthday
            SignificantDate? birthday = await context.Set<SignificantDate>().FirstOrDefaultAsync(d => d.EntityId == contact.Id && d.Title == "Birthday");
            Assert.NotNull(birthday);
            Assert.Equal(new DateTime(1990, 1, 1), birthday.Date);
        }

        [Fact]
        public async Task Delete_Post_ShouldDeleteContactAndDependencies()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, new Mock<IFileValidationService>().Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "To", LastName = "Delete" };
            context.Contacts.Add(contact);
            context.Set<Note>().Add(new Note { Id = Guid.NewGuid(), EntityId = contactId, EntityType = EntityTypes.Person, Title = "Note", Value = "Val" });
            await context.SaveChangesAsync();

            // Act
            IActionResult result = await controller.DeleteConfirmed(contactId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.Null(await context.Contacts.FindAsync(contactId));
            Assert.Empty(await context.Set<Note>().Where(n => n.EntityId == contactId).ToListAsync());
        }

        [Fact]
        public async Task Edit_Post_ShouldReturnValidationError_WhenFileIsNotImage()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns("test-user-id");
            userMock.Setup(u => u.UserName).Returns("Test User");

            Mock<IFileValidationService> fileValidationServiceMock = new();
            fileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(false);

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, fileValidationServiceMock.Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

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
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_ShouldReturnError_WhenFileSignatureInvalid()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns("test-user-id");
            userMock.Setup(u => u.UserName).Returns("Test User");

            Mock<IFileValidationService> fileValidationServiceMock = new();
            fileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            fileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, fileValidationServiceMock.Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

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
            fileMock.Setup(f => f.FileName).Returns("fake.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("image/png");

            // Act
            IActionResult result = await controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("Invalid file signature.", controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Edit_Post_ShouldSaveAttachment_WhenFileIsImage()
        {
            // Arrange
            using CRMDbContext context = GetInMemoryDbContext();
            Repository repository = new(context);
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            userMock.Setup(u => u.UserId).Returns("test-user-id");
            userMock.Setup(u => u.UserName).Returns("Test User");

            Mock<IFileValidationService> fileValidationServiceMock = new();
            fileValidationServiceMock.Setup(f => f.IsImageExtension(It.IsAny<string>())).Returns(true);
            fileValidationServiceMock.Setup(f => f.IsValidImageSignature(It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

            Mock<IUserSynchronizationService> syncMock = new();
            syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repository, loggerMock.Object, userMock.Object, new Mock<IVCardService>().Object, fileValidationServiceMock.Object, syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

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
