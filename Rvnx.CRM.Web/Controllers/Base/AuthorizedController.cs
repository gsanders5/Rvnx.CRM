using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Web.Filters;

namespace Rvnx.CRM.Web.Controllers.Base;

[TypeFilter(typeof(ConfigurableAuthorizeFilter))]
[AutoValidateAntiforgeryToken] // 🛡️ Sentinel: Global CSRF protection for all inheriting controllers
public abstract class AuthorizedController : Controller
{
}