using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public Guid? UserId
    {
        get
        {
            if (!IsAuthEnabled()) return null;

            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            // First, try to get the internal CRM user ID (set by UserSynchronizationService)
            string? internalId = user.FindFirst(ClaimConstants.InternalUserIdClaimType)?.Value;
            if (Guid.TryParse(internalId, out Guid internalGuid))
            {
                return internalGuid;
            }

            // Fallback to NameIdentifier for backwards compatibility
            // This handles cases where UserSynchronizationService hasn't run yet
            string? nameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(nameId, out Guid guid) ? guid : null;
        }
    }

    public string? UserName => !IsAuthEnabled()
                ? "System"
                : _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? "System";

    public bool IsAuthenticated => IsAuthEnabled() && (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

    private bool IsAuthEnabled()
    {
        return _configuration.GetValue<bool>("Authentication:Enabled");
    }
}