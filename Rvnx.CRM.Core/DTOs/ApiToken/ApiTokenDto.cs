namespace Rvnx.CRM.Core.DTOs.ApiToken;

public record ApiTokenDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    DateTime LastUsedAt,
    DateTime CreatedDate,
    bool IsActive
);
