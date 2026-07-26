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

        return GetGuidClaim(ClaimConstants.InternalGroupIdClaimType);
    }

    public async Task<bool> IsAdministratorAsync(Guid userId)
    {
        if (!IsAuthEnabled())
        {
            return false;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        // User is IGlobalEntity (no group query filter), so this filtered lookup resolves the
        // user even though no group context is established yet.
        User? user = await repo.GetByIdAsync<User>(userId);

        return user?.IsAdministrator ?? false;
    }

    private Guid? GetUserIdFromClaims()
    {
        if (!IsAuthEnabled())
        {
            return null;
        }

        return GetGuidClaim(ClaimConstants.InternalUserIdClaimType)
            ?? GetGuidClaim(ClaimTypes.NameIdentifier);
    }

    public string? UserName => !IsAuthEnabled() ? "System" :
        _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

    public string? DisplayName
    {
        get
        {
            if (!IsAuthEnabled())
            {
                return null;
            }

            return GetClaimValue(ClaimConstants.OidcNameClaimType)
                ?? GetClaimValue(ClaimTypes.Name)
                ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }
    }

    public string? Email
    {
        get
        {
            if (!IsAuthEnabled())
            {
                return null;
            }

            return GetClaimValue(ClaimTypes.Email)
                ?? GetClaimValue(ClaimConstants.OidcEmailClaimType);
        }
    }

    public bool IsAuthenticated => IsAuthEnabled() &&
        (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false);

    private bool IsAuthEnabled()
    {
        return _configuration.GetValue<bool>("Authentication:Enabled");
    }

    private string? GetClaimValue(string claimType) =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;

    private Guid? GetGuidClaim(string claimType) =>
        Guid.TryParse(GetClaimValue(claimType), out Guid guid) ? guid : null;
}
