using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Rvnx.CRM.API.Authentication;

public class ApiTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiToken";
    public const string ResolvedTokenItemKey = "ResolvedApiToken";
}

public class ApiTokenAuthenticationHandler : AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceProvider _serviceProvider;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<ApiTokenAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserService currentUserService,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _currentUserService = currentUserService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
        {
            return AuthenticateResult.NoResult();
        }

        string? authHeader = value.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string rawToken = authHeader["Bearer ".Length..].Trim();

        // Resolve token asynchronously and store it for the CurrentUserService to pick up
        // this avoids sync-over-async in the property getters of ICurrentUserService.
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IApiTokenService tokenService = scope.ServiceProvider.GetRequiredService<IApiTokenService>();
            Core.Models.ApiToken? resolvedToken = await tokenService.ResolveTokenAsync(rawToken);

            if (resolvedToken != null)
            {
                Context.Items[ApiTokenAuthenticationOptions.ResolvedTokenItemKey] = resolvedToken;
            }
        }

        // The token is validated implicitly by calling ICurrentUserService.IsAuthenticated
        // ApiTokenCurrentUserService retrieves it from the HttpContext and checks against DB.

        if (!_currentUserService.IsAuthenticated)
        {
            return AuthenticateResult.Fail("Invalid or missing API token.");
        }

        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserService.UserId.ToString() ?? string.Empty),
            new Claim(ClaimTypes.Name, _currentUserService.UserName ?? string.Empty)
        };

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}