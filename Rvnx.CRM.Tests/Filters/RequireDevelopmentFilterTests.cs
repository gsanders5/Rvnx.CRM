using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Moq;
using Rvnx.CRM.Web.Filters;

namespace Rvnx.CRM.Tests.Filters;

public class RequireDevelopmentFilterTests
{
    [Fact]
    public void OnAuthorizationReturnsNotFoundWhenNotDevelopment()
    {
        Mock<IHostEnvironment> environment = new();
        environment.Setup(e => e.EnvironmentName).Returns("Production");
        RequireDevelopmentFilter filter = new(environment.Object);
        AuthorizationFilterContext context = CreateContext();

        filter.OnAuthorization(context);

        Assert.IsType<NotFoundResult>(context.Result);
    }

    [Fact]
    public void OnAuthorizationAllowsRequestWhenDevelopment()
    {
        Mock<IHostEnvironment> environment = new();
        environment.Setup(e => e.EnvironmentName).Returns("Development");
        RequireDevelopmentFilter filter = new(environment.Object);
        AuthorizationFilterContext context = CreateContext();

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext()
    {
        ActionContext actionContext = new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
