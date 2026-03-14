using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Rvnx.CRM.API.Authentication;

public class ApiTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiToken";
}

public class ApiTokenAuthenticationHandler : AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly ICurrentUserService _currentUserService;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<ApiTokenAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICurrentUserService currentUserService)
        : base(options, logger, encoder)
    {
        _currentUserService = currentUserService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // The token is validated implicitly by calling ICurrentUserService.IsAuthenticated
        // ApiTokenCurrentUserService retrieves it from the HttpContext and checks against DB.

        if (!_currentUserService.IsAuthenticated)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid or missing API token."));
        }

        // We have a valid user
        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserService.UserId.ToString() ?? string.Empty),
            new Claim(ClaimTypes.Name, _currentUserService.UserName ?? string.Empty)
        };

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}