using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class DebugOperationsControllerTests : IDisposable
{
    private readonly CRMDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<DebugOperationsController>> _loggerMock;

    public DebugOperationsControllerTests()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dbContext = new CRMDbContext(options, _currentUserServiceMock.Object);
        _loggerMock = new Mock<ILogger<DebugOperationsController>>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void OnActionExecutingReturnsNotFoundWhenNotDevelopment()
    {
        Mock<IDebugDataService> debugServiceMock = new();
        Mock<IDebugOperationsService> debugOperationsServiceMock = new();
        Mock<IHostEnvironment> environmentMock = new();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");

        DebugOperationsController controller = new(debugServiceMock.Object, debugOperationsServiceMock.Object, environmentMock.Object, _currentUserServiceMock.Object);

        ActionContext actionContext = new(
            new DefaultHttpContext(),
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ControllerActionDescriptor()
        );

        ActionExecutingContext context = new(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller
        );

        controller.OnActionExecuting(context);

        Assert.IsType<NotFoundResult>(context.Result);
    }

    [Fact]
    public void OnActionExecutingAllowsExecutionWhenDevelopment()
    {
        Mock<IDebugDataService> debugServiceMock = new();
        Mock<IDebugOperationsService> debugOperationsServiceMock = new();
        Mock<IHostEnvironment> environmentMock = new();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

        DebugOperationsController controller = new(debugServiceMock.Object, debugOperationsServiceMock.Object, environmentMock.Object, _currentUserServiceMock.Object);

        ActionContext actionContext = new(
            new DefaultHttpContext(),
            new Microsoft.AspNetCore.Routing.RouteData(),
            new ControllerActionDescriptor()
        );

        ActionExecutingContext context = new(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller
        );

        controller.OnActionExecuting(context);

        Assert.Null(context.Result);
    }
}
