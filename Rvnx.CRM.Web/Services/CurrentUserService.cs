using System.Security.Claims;
using Rvnx.CRM.Core.Interfaces;

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

    public string? UserId
    {
        get
        {
            if (!IsAuthEnabled())
            {
                return null;
            }

            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    public string? UserName
    {
        get
        {
            if (!IsAuthEnabled())
            {
                return "System";
            }
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? "System";
        }
    }

    public bool IsAuthenticated => IsAuthEnabled() && (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

    private bool IsAuthEnabled()
    {
        return _configuration.GetValue<bool>("Authentication:Enabled");
    }
}
