using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rvnx.CRM.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private static readonly string[] AllowedUrlPrefixes =
    [
        "/",
        "/Home",
        "/Account",
        "/Attachments",
        "/Calendar",
        "/ContactMethods",
        "/Contacts",
        "/DebugOperations",
        "/Facts",
        "/Labels",
        "/Merge",
        "/Notes",
        "/Pets",
        "/Relationships",
        "/SignificantDates"
    ];

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        // Prevent Open Redirect vulnerability
        if (!IsUrlInSafelist(returnUrl))
        {
            returnUrl = "/";
        }

        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    private bool IsUrlInSafelist(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Standard ASP.NET Core check for local URLs
        if (!Url.IsLocalUrl(url))
        {
            return false;
        }

        // Defense-in-depth: Validate against a safelist of known application paths
        if (url == "/")
        {
            return true;
        }

        return AllowedUrlPrefixes.Any(prefix =>
            prefix != "/" &&
            url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            (url.Length == prefix.Length || url[prefix.Length] == '/' || url[prefix.Length] == '?'));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}