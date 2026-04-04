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
        // Prevent Open Redirect vulnerability
        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
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