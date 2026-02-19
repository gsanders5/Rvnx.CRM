using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rvnx.CRM.Web.Filters;

public class ConfigurableAuthorizeFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if authorization is enabled globally
        if (!_configuration.GetValue<bool>("Authentication:Enabled"))
        {
            return Task.CompletedTask;
        }

        // Check if user is authenticated
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
        }

        return Task.CompletedTask;
    }
}
