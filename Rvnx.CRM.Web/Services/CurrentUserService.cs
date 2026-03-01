using Microsoft.EntityFrameworkCore;
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

    private Guid? _groupId;
    private bool _groupIdResolved;

    public Guid? UserId => GetUserIdFromClaims();

    public Guid? GroupId
    {
        get
        {
            if (_groupIdResolved)
            {
                return _groupId;
            }

            if (!IsAuthEnabled() || UserId == null)
            {
                _groupIdResolved = true;
                return _groupId = null;
            }

            // Clean resolution via ServiceProvider breaks the circularity
            using IServiceScope scope = _serviceProvider.CreateScope();
            IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();

            // Query logic using your existing architecture
            var user = repo.QueryUnfiltered<User>()
                           .Select(u => new { u.Id, u.GroupId })
                           .FirstOrDefault(u => u.Id == UserId);

            _groupId = user?.GroupId;
            _groupIdResolved = true;
            return _groupId;
        }
    }

    public async Task<bool> IsAdministratorAsync(Guid userId)
    {
        if (!IsAuthEnabled())
        {
            return false;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        User? user = await repo.QueryUnfiltered<User>().FirstOrDefaultAsync(u => u.Id == userId);

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