using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
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
}
