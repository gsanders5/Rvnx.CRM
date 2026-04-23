using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rvnx.CRM.Web.Filters;

public class RequireDevelopmentFilter(IHostEnvironment environment) : IAuthorizationFilter
{
    private readonly IHostEnvironment _environment = environment;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!_environment.IsDevelopment())
        {
            // 404 rather than 403 so dev-only endpoints are indistinguishable from non-existent routes in production.
            context.Result = new NotFoundResult();
        }
    }
}
