using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rvnx.CRM.Web.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken] // 🛡️ Sentinel: Global CSRF protection since this doesn't inherit from AuthorizedController
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        // Prevent Open Redirect vulnerability using defense-in-depth:
        // 1. IsLocalUrl ensures it's not an absolute external URL.
        // 2. IsUrlInSafelist ensures it only redirects to allowed paths within the app.
        if (!Url.IsLocalUrl(returnUrl) || !IsUrlInSafelist(returnUrl))
        {
            returnUrl = "/";
        }

        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    private static bool IsUrlInSafelist(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        string[] safelist = { "/", "/Home", "/Account", "/Contacts", "/Facts" };

        string pathOnly = url;
        int queryIndex = url.IndexOf('?');
        if (queryIndex >= 0)
        {
            pathOnly = url[..queryIndex];
        }

        return Array.Exists(safelist, safeUrl =>
            pathOnly.Equals(safeUrl, StringComparison.OrdinalIgnoreCase) ||
            pathOnly.StartsWith($"{safeUrl}/", StringComparison.OrdinalIgnoreCase));
    }

    [HttpPost]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}