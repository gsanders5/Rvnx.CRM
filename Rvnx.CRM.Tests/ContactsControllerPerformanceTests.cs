using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Web.Controllers;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests
{
    public class ContactsControllerPerformanceTests
    {
        [Fact]
        public async Task Index_WithSmallNumberOfContacts_UsesOptimizedQueryWithContains()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();

            // Return 10 contacts (small set)
            List<Contact> contacts = Enumerable.Range(0, 10)
                .Select(i => new Contact { Id = Guid.NewGuid(), FirstName = $"First{i}", LastName = $"Last{i}" })
                .ToList();

            repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contacts);

            repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Attachment>());

            ContactsController controller = new(repositoryMock.Object, loggerMock.Object, userMock.Object, Mock.Of<IVCardService>(), Mock.Of<IFileValidationService>(), syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            // Act
            await controller.Index();

            // Assert
            // Verify that ListAsNoTrackingAsync<Attachment> was called with an expression that uses "Contains" (filtering by IDs)
            repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Attachment>(
                It.Is<Expression<Func<Attachment, bool>>>(expr => expr.ToString().Contains("Contains")),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task Index_WithLargeNumberOfContacts_AvoidsSqlParameterLimit()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<ILogger<ContactsController>> loggerMock = new();
            Mock<ICurrentUserService> userMock = new();
            Mock<IUserSynchronizationService> syncMock = new();

            // Return 2500 contacts (large set > 2100)
            List<Contact> contacts = Enumerable.Range(0, 2500)
                .Select(i => new Contact { Id = Guid.NewGuid(), FirstName = $"First{i}", LastName = $"Last{i}" })
                .ToList();

            repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(contacts);

            repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Attachment>());

            ContactsController controller = new(repositoryMock.Object, loggerMock.Object, userMock.Object, Mock.Of<IVCardService>(), Mock.Of<IFileValidationService>(), syncMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            // Act
            await controller.Index();

            // Assert
            // Verify that ListAsNoTrackingAsync<Attachment> was called with an expression that does NOT use "Contains"
            // This ensures we fetch purely by EntityType and filter in memory, avoiding the SQL limit.
            repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Attachment>(
                It.Is<Expression<Func<Attachment, bool>>>(expr => !expr.ToString().Contains("Contains")),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once);
        }
    }
}
