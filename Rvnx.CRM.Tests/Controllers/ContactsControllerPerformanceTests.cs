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
        public async Task IndexDelegatesToHasAnyContacts()
        {
            // Arrange
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            Mock<IContactReadService> readServiceMock = new();

            readServiceMock.Setup(s => s.HasAnyContactsAsync(It.IsAny<bool>())).ReturnsAsync(true);

            ContactsController controller = new(loggerMock.Object, userMock.Object, Mock.Of<IContactImportService>(), Mock.Of<IContactExportService>(), Mock.Of<IContactManagementService>(), readServiceMock.Object, Mock.Of<ISelfContactService>())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            // Act
            await controller.Index();

            // Assert
            readServiceMock.Verify(s => s.HasAnyContactsAsync(false), Times.Once);
        }
    }
}
