using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Repositories
{
    public class CascadeDeleteTests
    {
        [Fact]
        public async Task DeleteContact_ShouldDelete_Dependencies()
        {
            // Arrange
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IContactManagementService> managementMock = new();

            managementMock.Setup(m => m.DeleteContactAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            ContactsController controller = new(logger.Object, userMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, managementMock.Object, new Mock<IContactReadService>().Object, new Mock<ISelfContactService>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Guid contactId = Guid.NewGuid();

            // Act
            await controller.DeleteConfirmed(contactId);

            // Assert
            managementMock.Verify(m => m.DeleteContactAsync(contactId), Times.Once);
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Relationships()
        {
            // Arrange
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IContactManagementService> managementMock = new();

            managementMock.Setup(m => m.DeleteContactAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            ContactsController controller = new(logger.Object, userMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, managementMock.Object, new Mock<IContactReadService>().Object, new Mock<ISelfContactService>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Guid contactId = Guid.NewGuid();

            // Act
            await controller.DeleteConfirmed(contactId);

            // Assert
            managementMock.Verify(m => m.DeleteContactAsync(contactId), Times.Once);
        }
    }
}
