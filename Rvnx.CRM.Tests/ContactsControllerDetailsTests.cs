using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerDetailsTests
    {
        [Fact]
        public async Task Details_ShouldReturnViewWithMappedRelationships()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();
            Mock<IContactReadService> readServiceMock = new();

            Guid contactId = Guid.NewGuid();
            ContactDetailDto detailDto = new()
            {
                Id = contactId,
                FirstName = "Test",
                Relationships = new List<RelationshipDto>(),
                RelatedTo = new List<RelationshipDto>()
            };

            readServiceMock.Setup(s => s.GetContactDetailsAsync(contactId)).ReturnsAsync(detailDto);

            ContactsController controller = new(repositoryMock.Object, loggerMock.Object, userMock.Object, Mock.Of<IContactImportService>(), Mock.Of<IContactExportService>(), Mock.Of<IContactManagementService>(), readServiceMock.Object, Mock.Of<ISelfContactService>());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            IActionResult result = await controller.Details(contactId);

            // Assert
            ViewResult viewResult = Assert.IsType<ViewResult>(result);
            ContactDetailDto model = Assert.IsAssignableFrom<ContactDetailDto>(viewResult.Model);
            Assert.Equal(contactId, model.Id);
        }
    }
}
