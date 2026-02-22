using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class DebugOperationsControllerTests
    {
        [Fact]
        public void OnActionExecutingReturnsNotFoundWhenNotDevelopment()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<IHostEnvironment> environmentMock = new();
            environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

            DebugOperationsController controller = new(repositoryMock.Object, environmentMock.Object);

            ActionContext actionContext = new(
                new DefaultHttpContext(),
                new Microsoft.AspNetCore.Routing.RouteData(),
                new ControllerActionDescriptor()
            );

            ActionExecutingContext context = new(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller
            );

            // Act
            controller.OnActionExecuting(context);

            // Assert
            Assert.IsType<NotFoundResult>(context.Result);
        }

        [Fact]
        public void OnActionExecutingAllowsExecutionWhenDevelopment()
        {
            // Arrange
            Mock<IRepository> repositoryMock = new();
            Mock<IHostEnvironment> environmentMock = new();
            environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

            DebugOperationsController controller = new(repositoryMock.Object, environmentMock.Object);

            ActionContext actionContext = new(
                new DefaultHttpContext(),
                new Microsoft.AspNetCore.Routing.RouteData(),
                new ControllerActionDescriptor()
            );

            ActionExecutingContext context = new(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller
            );

            // Act
            controller.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }
    }
}
