using Microsoft.AspNetCore.Authentication;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Security.Claims;

namespace Rvnx.CRM.Web.Security;

public class UserClaimsTransformation(IServiceProvider serviceProvider, IConfiguration configuration) : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;

    private const string TransformationProcessedKey = "Rvnx_ClaimsTransformed";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (!_configuration.GetValue<bool>("Authentication:Enabled"))
        {
            return principal;
        }

        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        // Optimization: Don't transform multiple times per request if middleware clones the principal
        if (identity.HasClaim(c => c.Type == TransformationProcessedKey))
        {
            return principal;
        }

        if (identity.HasClaim(c => c.Type == ClaimConstants.InternalUserIdClaimType))
        {
            identity.AddClaim(new Claim(TransformationProcessedKey, "true"));
            return principal;
        }

        string? subjectId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(subjectId))
        {
            return principal;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();

        var user = (await repo.ListAsNoTrackingAsync<User>(u => u.SubjectId == subjectId)).FirstOrDefault();

        if (user != null)
        {
            identity.AddClaim(new Claim(ClaimConstants.InternalUserIdClaimType, user.Id.ToString()));

            if (user.GroupId.HasValue)
            {
                identity.AddClaim(new Claim(ClaimConstants.InternalGroupIdClaimType, user.GroupId.Value.ToString()));
            }
        }

        identity.AddClaim(new Claim(TransformationProcessedKey, "true"));
        return principal;
    }
}
