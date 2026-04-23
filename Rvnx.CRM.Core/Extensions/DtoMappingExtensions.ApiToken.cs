using Rvnx.CRM.Core.DTOs.ApiToken;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Core.Extensions;

public static partial class DtoMappingExtensions
{
    public static ApiTokenDto ToDto(this ApiToken entity)
    {
        return new ApiTokenDto(
            entity.Id,
            entity.Name,
            entity.TokenPrefix,
            entity.ExpiresAt,
            entity.RevokedAt,
            entity.LastUsedAt,
            entity.CreatedDate,
            entity.IsActive
        );
    }
}
