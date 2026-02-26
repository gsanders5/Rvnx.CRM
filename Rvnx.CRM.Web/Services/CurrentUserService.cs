using Microsoft.Data.Sqlite;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;
    private Guid? _groupId;
    private bool _groupIdResolved;

    public Guid? UserId
    {
        get
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

    public Guid? GroupId
    {
        get
        {
            if (_groupIdResolved)
            {
                return _groupId;
            }

            if (!IsAuthEnabled())
            {
                _groupIdResolved = true;
                _groupId = null;
                return null;
            }

            Guid? userId = UserId;
            if (userId == null)
            {
                _groupIdResolved = true;
                _groupId = null;
                return null;
            }

            _groupId = ResolveGroupIdFromDb(userId.Value);
            _groupIdResolved = true;
            return _groupId;
        }
    }

    private Guid? ResolveGroupIdFromDb(Guid userId)
    {
        try
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString)) return null;

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT GroupId FROM Users WHERE Id = @UserId";
            command.Parameters.AddWithValue("@UserId", userId);

            object? result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                if (Guid.TryParse(result.ToString(), out Guid groupId))
                {
                    return groupId;
                }
                if (result is byte[] bytes && bytes.Length == 16)
                {
                    return new Guid(bytes);
                }
            }
            return null;
        }
        catch
        {
            return null;
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