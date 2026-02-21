using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class CascadeDeleteTests
    {
        private CRMDbContext GetInMemoryDbContext()
        {
            DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Mock<ICurrentUserService> mockUserService = new();
            mockUserService.Setup(u => u.UserId).Returns((Guid?) null);
            mockUserService.Setup(u => u.UserName).Returns("TestUser");

            CRMDbContext context = new(options, mockUserService.Object);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task DeleteContact_ShouldDelete_Dependencies()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IContactManagementService> managementMock = new();

            managementMock.Setup(m => m.DeleteContactAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repositoryMock.Object, logger.Object, userMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, managementMock.Object, new Mock<IContactReadService>().Object, new Mock<ISelfContactService>().Object);
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
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> logger = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IContactManagementService> managementMock = new();

            managementMock.Setup(m => m.DeleteContactAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            ContactsController controller = new(repositoryMock.Object, logger.Object, userMock.Object, new Mock<IContactImportService>().Object, new Mock<IContactExportService>().Object, managementMock.Object, new Mock<IContactReadService>().Object, new Mock<ISelfContactService>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            Guid contactId = Guid.NewGuid();

            // Act
            await controller.DeleteConfirmed(contactId);

            // Assert
            managementMock.Verify(m => m.DeleteContactAsync(contactId), Times.Once);
        }
    }
}
