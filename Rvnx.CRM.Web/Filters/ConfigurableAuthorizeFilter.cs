using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rvnx.CRM.Web.Filters;

public class ConfigurableAuthorizeFilter(IConfiguration configuration) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_configuration.GetValue<bool>("Authentication:Enabled"))
        {
            return Task.CompletedTask;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
        }

        return Task.CompletedTask;
    }
}