using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IServiceProvider serviceProvider) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Guid? UserId => GetUserIdFromClaims();
    public Guid? GroupId => GetGroupIdFromClaims();

    private Guid? GetGroupIdFromClaims()
    {
        if (!IsAuthEnabled())
        {
            return null;
        }

        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        string? groupId = user?.FindFirst(ClaimConstants.InternalGroupIdClaimType)?.Value;

        return Guid.TryParse(groupId, out Guid guid) ? guid : null;
    }

    public async Task<bool> IsAdministratorAsync(Guid userId)
    {
        if (!IsAuthEnabled())
        {
            return false;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        User? user = await repo.GetByIdAsync<User>(userId);

        return user?.IsAdministrator ?? false;
    }

    private Guid? GetUserIdFromClaims()
    {
        if (!IsAuthEnabled())
        {
            return null;
        }

        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return null;
        }

        string? internalId = user.FindFirst(ClaimConstants.InternalUserIdClaimType)?.Value;
        if (Guid.TryParse(internalId, out Guid internalGuid))
        {
            return internalGuid;
        }

        string? nameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(nameId, out Guid guid) ? guid : null;
    }

    public string? UserName => !IsAuthEnabled() ? "System" :
        _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

    public bool IsAuthenticated => IsAuthEnabled() &&
        (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

    private bool IsAuthEnabled()
    {
        return _configuration.GetValue<bool>("Authentication:Enabled");
    }
}
