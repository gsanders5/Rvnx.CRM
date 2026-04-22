using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Web.Filters;

namespace Rvnx.CRM.Web.Controllers.Base;

[TypeFilter(typeof(ConfigurableAuthorizeFilter))]
[AutoValidateAntiforgeryToken] // 🛡️ Sentinel: Global CSRF protection for all inheriting controllers
public abstract class AuthorizedController : Controller
{
    protected IActionResult SafeRedirect(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl!);
        }

        string referer = Request.Headers.Referer.ToString();
        return Uri.TryCreate(referer, UriKind.Absolute, out Uri? uri) &&
               string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            ? LocalRedirect(uri.PathAndQuery)
            : RedirectToAction("Index", "Home");
    }
}
