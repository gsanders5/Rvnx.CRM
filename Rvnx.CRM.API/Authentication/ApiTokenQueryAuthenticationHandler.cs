using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Rvnx.CRM.API.Authentication;

public class ApiTokenQueryAuthenticationHandler : AuthenticationHandler<ApiTokenQueryAuthenticationOptions>
{
    private const string TokenQueryKey = "token";

    private readonly IServiceProvider _serviceProvider;

    public ApiTokenQueryAuthenticationHandler(
        IOptionsMonitor<ApiTokenQueryAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Query.TryGetValue(TokenQueryKey, out Microsoft.Extensions.Primitives.StringValues value))
        {
            return AuthenticateResult.NoResult();
        }

        string rawToken = value.ToString().Trim();
        if (string.IsNullOrEmpty(rawToken))
        {
            return AuthenticateResult.NoResult();
        }

        Core.Models.ApiToken? resolvedToken;
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IApiTokenService tokenService = scope.ServiceProvider.GetRequiredService<IApiTokenService>();
            resolvedToken = await tokenService.ResolveTokenAsync(rawToken);
        }

        if (resolvedToken == null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        Context.Items["ResolvedApiToken"] = resolvedToken;

        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, resolvedToken.UserId.ToString()),
            new Claim(ClaimTypes.Name, resolvedToken.Name ?? string.Empty),
        };

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
