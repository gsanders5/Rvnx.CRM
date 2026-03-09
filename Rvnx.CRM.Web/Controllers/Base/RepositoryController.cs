using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Web.Controllers.Base;

public abstract class RepositoryController : AuthorizedController
{
    // The repository is no longer directly exposed to derived controllers for business logic.
    // It is kept private for the base class helper methods only.
    private readonly IRepository _repository;

    protected RepositoryController(IRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Checks if a contact exists and is not a partial contact.
    /// Use this for validating entity references in create/edit actions.
    /// </summary>
    protected async Task<bool> IsValidContactAsync(Guid id)
    {
        return await _repository.IsValidContactAsync(id);
    }

    protected IActionResult RedirectToEntity(Guid id, string? type)
    {
        return id == Guid.Empty || string.IsNullOrEmpty(type)
            ? RedirectToAction("Index", "Home")
            : type == EntityTypes.Person ? RedirectToAction("Details", "Contacts", new { id }) : RedirectToAction("Index", "Home");
    }
}
