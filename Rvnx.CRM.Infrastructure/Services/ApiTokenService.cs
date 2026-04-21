using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace Rvnx.CRM.Infrastructure.Services;

public class ApiTokenService(IRepository repository) : IApiTokenService
{
    private readonly IRepository _repository = repository;

    public async Task<(ApiToken token, string rawToken)> CreateTokenAsync(Guid userId, Guid groupId, string name, DateTime? expiresAt)
    {
        byte[] randomBytes = new byte[30];
        RandomNumberGenerator.Fill(randomBytes);

        string rawTokenBase64 = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        string rawToken = $"crm_{rawTokenBase64}";
        string tokenHash = ComputeHash(rawToken);
        string tokenPrefix = rawToken[..8];

        ApiToken token = new()
        {
            UserId = userId,
            GroupId = groupId,
            Name = name,
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = expiresAt,
            LastUsedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(token);
        await _repository.SaveChangesAsync();

        return (token, rawToken);
    }

    public async Task<bool> RevokeTokenAsync(Guid tokenId, Guid userId)
    {
        ApiToken? token = await _repository.QueryUnfiltered<ApiToken>()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId);

        if (token == null || token.RevokedAt != null)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(token);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<ApiToken?> ResolveTokenAsync(string rawToken)
    {
        string tokenHash = ComputeHash(rawToken);

        ApiToken? token = await _repository.QueryUnfiltered<ApiToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        return token == null || !token.IsActive ? null : token;
    }

    public async Task<IEnumerable<ApiToken>> ListTokensAsync(Guid userId)
    {
        return await _repository.QueryUnfiltered<ApiToken>()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    private static string ComputeHash(string rawToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
