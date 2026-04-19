using Microsoft.AspNetCore.Authentication;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Extensions;
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

        using IServiceScope scope = _serviceProvider.CreateScope();
        IUserSynchronizationService userSyncService = scope.ServiceProvider.GetRequiredService<IUserSynchronizationService>();

        await identity.SyncUserAndEnrichClaimsAsync(userSyncService);

        identity.AddClaim(new Claim(TransformationProcessedKey, "true"));
        return principal;
    }
}