using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerTests : IDisposable
    {
        private readonly Mock<ILogger<ContactsController>> _loggerMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IUserSynchronizationService> _syncMock = new();
        private readonly Mock<IContactImportService> _contactImportServiceMock = new();
        private readonly Mock<IContactExportService> _contactExportServiceMock = new();
        private readonly Mock<IContactManagementService> _contactManagementServiceMock = new();
        private readonly Mock<IContactReadService> _contactReadServiceMock = new();
        private readonly Mock<ISelfContactService> _selfContactServiceMock = new();
        private readonly Mock<IRepository> _repositoryMock = new();
        private readonly ContactsController _controller;

        public ContactsControllerTests()
        {
            _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            _userMock.Setup(s => s.UserName).Returns("test-user");
            _userMock.Setup(s => s.IsAuthenticated).Returns(true);

            _syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);

            _controller = new ContactsController(
                _repositoryMock.Object,
                _loggerMock.Object,
                _userMock.Object,
                _contactImportServiceMock.Object,
                _contactExportServiceMock.Object,
                _contactManagementServiceMock.Object,
                _contactReadServiceMock.Object,
                _selfContactServiceMock.Object);

            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task Index_ReturnsViewWithContacts()
        {
            // Arrange
            List<ContactDto> contacts = new()
            {
                new ContactDto { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                new ContactDto { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" }
            };

            _contactReadServiceMock.Setup(s => s.GetIndexDataAsync(It.IsAny<bool>())).ReturnsAsync(contacts);

            // Act
            IActionResult result = await _controller.Index();

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactIndexViewModel model = Assert.IsAssignableFrom<ContactIndexViewModel>(viewResult.Model);
            Assert.Equal(2, model.Contacts.Count());
        }

        [Fact]
        public async Task Create_Post_WithValidData_ShouldCreateContactAndRelatedEntities()
        {
            // Arrange
            ContactCreateViewModel dto = new()
            {
                FirstName = "New",
                LastName = "User",
                Email = "new@user.com",
                Phone = "1234567890",
                Birthday = new DateTime(1990, 1, 1)
            };

            _contactManagementServiceMock.Setup(s => s.CreateContactAsync(It.IsAny<ContactFormDto>()))
                .ReturnsAsync(ContactOperationResult.Ok(Guid.NewGuid()));

            // Act
            IActionResult result = await _controller.Create(dto);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ShouldCallServiceAndRedirect()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();

            // Act
            IActionResult result = await _controller.DeleteConfirmed(contactId);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            _contactManagementServiceMock.Verify(s => s.DeleteContactAsync(contactId), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_WhenFileIsNotImage_ShouldReturnValidationError()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            ContactFormDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

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

            // Mock Exist to return true so we don't get NotFound
            _contactReadServiceMock.Setup(s => s.ContactExistsAsync(contactId)).ReturnsAsync(true);

            _contactManagementServiceMock.Setup(s => s.UpdateContactAsync(It.IsAny<Guid>(), It.IsAny<ContactFormDto>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ContactOperationResult.Failure("Only image files (jpg, jpeg, png, gif) are allowed."));

            // Act
            IActionResult result = await _controller.Edit(contactId, dto, fileMock.Object);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result); // Expecting view result with errors
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Import_WhenServiceSucceeds_ShouldRedirectToIndex()
        {
            // Arrange
            ContactImportResult importResult = new() { AddedCount = 1, SkippedCount = 1 };
            _contactImportServiceMock.Setup(s => s.ImportFromVCardAsync(It.IsAny<Stream>()))
                .ReturnsAsync(importResult);

            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("contacts.vcf");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            // Act
            IActionResult result = await _controller.Import(fileMock.Object);

            // Assert
            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify message
            Assert.Equal("Import successful! Added: 1, Skipped: 1", _controller.TempData["SuccessMessage"]);
        }
    }
}
