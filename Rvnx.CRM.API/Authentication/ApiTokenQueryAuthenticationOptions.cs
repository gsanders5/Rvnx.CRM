using Microsoft.AspNetCore.Authentication;

namespace Rvnx.CRM.API.Authentication;

public class ApiTokenQueryAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiTokenQuery";
}
