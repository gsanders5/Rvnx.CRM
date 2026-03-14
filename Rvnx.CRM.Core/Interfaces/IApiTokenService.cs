using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Core.Interfaces;

public interface IApiTokenService
{
    Task<(ApiToken token, string rawToken)> CreateTokenAsync(Guid userId, Guid groupId, string name, DateTime? expiresAt);
    Task<bool> RevokeTokenAsync(Guid tokenId, Guid userId);
    Task<ApiToken?> ResolveTokenAsync(string rawToken);
    Task<IEnumerable<ApiToken>> ListTokensAsync(Guid userId);
}