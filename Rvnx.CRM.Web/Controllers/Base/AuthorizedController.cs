using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Web.Filters;

namespace Rvnx.CRM.Web.Controllers.Base;

[TypeFilter(typeof(ConfigurableAuthorizeFilter))]
[AutoValidateAntiforgeryToken]
public abstract class AuthorizedController : Controller
{
}