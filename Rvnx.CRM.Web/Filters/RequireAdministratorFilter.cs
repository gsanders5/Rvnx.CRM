using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Web.Filters;

public class RequireAdministratorFilter(
    IConfiguration configuration,
    ICurrentUserService currentUserService,
    IDebugOperationsService debugOperationsService) : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IDebugOperationsService _debugOperationsService = debugOperationsService;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        bool authEnabled = _configuration.GetValue<bool>("Authentication:Enabled");

        if (authEnabled && context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // Regardless of auth config, require a valid admin user
        Guid? userId = _currentUserService.UserId;
        if (userId == null || !await _currentUserService.IsAdministratorAsync(userId.Value))
        {
            context.Result = new ForbidResult();
        }
    }
}