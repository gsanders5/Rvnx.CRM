using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Web.ViewModels.Contact;

namespace Rvnx.CRM.Tests.Controllers;

public class ContactsControllerTests
{
    public class ContactsControllerDetailsTests
    {

        [Fact]
        public async Task DetailsShouldReturnViewWithMappedRelationships()
        {
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            Mock<IContactReadService> readServiceMock = new();

            Guid contactId = Guid.NewGuid();
            ContactDetailDto detailDto = new()
            {
                Id = contactId,
                FirstName = "Test",
                Relationships = [],
                RelatedTo = []
            };

            readServiceMock.Setup(s => s.GetContactDetailsAsync(contactId)).ReturnsAsync(detailDto);

            ContactsController controller = new(loggerMock.Object, userMock.Object, Mock.Of<IContactImportService>(), Mock.Of<IContactExportService>(), Mock.Of<IContactManagementService>(), readServiceMock.Object, Mock.Of<ISelfContactService>(), Mock.Of<IFileValidationService>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            IActionResult result = await controller.Details(contactId);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactDetailDto model = Assert.IsAssignableFrom<ContactDetailDto>(viewResult.Model);
            Assert.Equal(contactId, model.Id);
        }

    }
    public class ContactsControllerPerformanceTests
    {

        [Fact]
        public async Task IndexDelegatesToService()
        {
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            Mock<IContactReadService> readServiceMock = new();

            List<ContactDto> contactDtos =
            [
                new ContactDto { Id = Guid.NewGuid(), FirstName = "Test" }
            ];

            readServiceMock.Setup(s => s.GetIndexDataAsync(It.IsAny<bool>())).ReturnsAsync(contactDtos);

            ContactsController controller = new(loggerMock.Object, userMock.Object, Mock.Of<IContactImportService>(), Mock.Of<IContactExportService>(), Mock.Of<IContactManagementService>(), readServiceMock.Object, Mock.Of<ISelfContactService>(), Mock.Of<IFileValidationService>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            await controller.Index();

            readServiceMock.Verify(s => s.GetIndexDataAsync(false), Times.Once);
        }

    }
    public class General : IDisposable
    {

        private readonly Mock<ILogger<ContactsController>> _loggerMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly Mock<IUserSynchronizationService> _syncMock = new();
        private readonly Mock<IContactImportService> _contactImportServiceMock = new();
        private readonly Mock<IContactExportService> _contactExportServiceMock = new();
        private readonly Mock<IContactManagementService> _contactManagementServiceMock = new();
        private readonly Mock<IContactReadService> _contactReadServiceMock = new();
        private readonly Mock<ISelfContactService> _selfContactServiceMock = new();
        private readonly Mock<IFileValidationService> _fileValidationServiceMock = new();
        private readonly ContactsController _controller;

        public General()
        {
            _userMock.Setup(s => s.UserId).Returns(Guid.Parse("c5b50a20-34b2-44b2-8b9c-aa4135f60938"));
            _userMock.Setup(s => s.UserName).Returns("test-user");
            _userMock.Setup(s => s.IsAuthenticated).Returns(true);

            _syncMock.Setup(s => s.SyncUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(Task.CompletedTask);
            _fileValidationServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(true);

            _controller = new ContactsController(
                _loggerMock.Object,
                _userMock.Object,
                _contactImportServiceMock.Object,
                _contactExportServiceMock.Object,
                _contactManagementServiceMock.Object,
                _contactReadServiceMock.Object,
                _selfContactServiceMock.Object,
                _fileValidationServiceMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task IndexReturnsViewWithContacts()
        {
            List<ContactDto> contacts =
            [
                new ContactDto { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" },
                    new ContactDto { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe" }
            ];

            _contactReadServiceMock.Setup(s => s.GetIndexDataAsync(It.IsAny<bool>())).ReturnsAsync(contacts);

            IActionResult result = await _controller.Index();

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactIndexViewModel model = Assert.IsAssignableFrom<ContactIndexViewModel>(viewResult.Model);
            Assert.Equal(2, model.Contacts.Count());
        }

        [Fact]
        public async Task CreatePostWithValidDataShouldCreateContactAndRelatedEntities()
        {
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

            IActionResult result = await _controller.Create(dto);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmedShouldCallServiceAndRedirect()
        {
            Guid contactId = Guid.NewGuid();

            IActionResult result = await _controller.DeleteConfirmed(contactId);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            _contactManagementServiceMock.Verify(s => s.DeleteContactAsync(contactId), Times.Once);
        }

        [Fact]
        public async Task EditPostWhenFileIsNotImageShouldReturnValidationError()
        {
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

            _contactReadServiceMock.Setup(s => s.ContactExistsAsync(contactId)).ReturnsAsync(true);

            _contactManagementServiceMock.Setup(s => s.UpdateContactAsync(It.IsAny<Guid>(), It.IsAny<ContactFormDto>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ContactOperationResult.Failure("Only image files (jpg, jpeg, png, gif) are allowed."));

            IActionResult result = await _controller.Edit(contactId, dto, fileMock.Object);

            ViewResult viewResult = Assert.IsType<ViewResult>(result); // Expecting view result with errors
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("Only image files (jpg, jpeg, png, gif) are allowed.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task ImportWhenServiceSucceedsShouldRedirectToIndex()
        {
            ContactImportResult importResult = new() { AddedCount = 1, SkippedCount = 1 };
            _contactImportServiceMock.Setup(s => s.ImportFromVCardAsync(It.IsAny<Stream>()))
                .ReturnsAsync(importResult);

            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("contacts.vcf");
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            IActionResult result = await _controller.Import(fileMock.Object);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.Equal("Import successful! Added: 1, Skipped: 1", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task ImportWhenFileIsTooLargeShouldReturnError()
        {
            Mock<IFormFile> fileMock = new();
            fileMock.Setup(f => f.Length).Returns(31 * 1024 * 1024); // 31 MB
            fileMock.Setup(f => f.FileName).Returns("contacts.vcf");

            _fileValidationServiceMock.Setup(s => s.IsAllowedFileSize(It.IsAny<long>())).Returns(false);

            IActionResult result = await _controller.Import(fileMock.Object);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("File is too large.", _controller.ModelState["file"]!.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task DeleteReturnsViewWithContactDetailDto()
        {
            Guid contactId = Guid.NewGuid();
            ContactDetailDto contactDto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };
            _contactReadServiceMock.Setup(s => s.GetContactDetailsAsync(contactId)).ReturnsAsync(contactDto);

            IActionResult result = await _controller.Delete(contactId);

            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactDetailDto model = Assert.IsAssignableFrom<ContactDetailDto>(viewResult.Model);
            Assert.Equal(contactId, model.Id);
            Assert.Equal("John", model.FirstName);
        }

        [Fact]
        public async Task EditPostWhenContactNotFoundShouldReturnNotFound()
        {
            Guid contactId = Guid.NewGuid();
            ContactFormDto dto = new() { Id = contactId, FirstName = "John", LastName = "Doe" };

            _contactManagementServiceMock.Setup(s => s.UpdateContactAsync(
                    contactId, dto, It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ContactOperationResult.NotFound());

            IActionResult result = await _controller.Edit(contactId, dto, null);

            Assert.IsType<NotFoundResult>(result);
        }
        [Fact]
        public async Task DetailsWhenServiceReturnsNullReturnsNotFound()
        {
            Guid contactId = Guid.NewGuid();
            _contactReadServiceMock.Setup(s => s.GetContactDetailsAsync(contactId)).ReturnsAsync((ContactDetailDto?)null);

            IActionResult result = await _controller.Details(contactId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditGetWhenServiceReturnsNullReturnsNotFound()
        {
            Guid contactId = Guid.NewGuid();
            _contactReadServiceMock.Setup(s => s.GetContactFormAsync(contactId)).ReturnsAsync((ContactFormDto?)null);

            IActionResult result = await _controller.Edit(contactId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AssignLabelRedirectsToEdit()
        {
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Mock<ILabelService> mockLabelService = new();
            mockLabelService.Setup(s => s.AssignLabelAsync(contactId, labelId)).Returns(Task.CompletedTask);

            Mock<IUrlHelper> mockUrlHelper = new();
            mockUrlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
            _controller.Url = mockUrlHelper.Object;

            IActionResult result = await _controller.AssignLabel(contactId, labelId, mockLabelService.Object);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            Assert.Equal(contactId, redirectResult.RouteValues!["id"]);
            mockLabelService.Verify(s => s.AssignLabelAsync(contactId, labelId), Times.Once);
        }

        [Fact]
        public async Task RemoveLabelRedirectsToEdit()
        {
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Mock<ILabelService> mockLabelService = new();
            mockLabelService.Setup(s => s.RemoveLabelAsync(contactId, labelId)).Returns(Task.CompletedTask);

            Mock<IUrlHelper> mockUrlHelper = new();
            mockUrlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
            _controller.Url = mockUrlHelper.Object;

            IActionResult result = await _controller.RemoveLabel(contactId, labelId, mockLabelService.Object);

            RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Edit", redirectResult.ActionName);
            Assert.Equal(contactId, redirectResult.RouteValues!["id"]);
            mockLabelService.Verify(s => s.RemoveLabelAsync(contactId, labelId), Times.Once);
        }

    }
}