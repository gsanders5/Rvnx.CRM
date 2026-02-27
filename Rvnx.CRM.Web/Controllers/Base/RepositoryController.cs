using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Constants;
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

    protected async Task<string> GetEntityName(Guid id, string type)
    {
        if (type == EntityTypes.Person)
        {
            Contact? p = await _repository.GetByIdAsync<Contact>(id);
            return p?.FullName ?? "Unknown Person";
        }
        else if (type == EntityTypes.Company)
        {
            Employer? c = await _repository.GetByIdAsync<Employer>(id);
            return c?.CompanyName ?? "Unknown Company";
        }
        return "Unknown Entity";
    }

    protected async Task<bool> IsPartialContactAsync(Guid id)
    {
        Contact? c = await _repository.GetByIdAsync<Contact>(id);
        return c?.IsPartial == true;
    }

    /// <summary>
    /// Checks if a contact exists and is not a partial contact.
    /// Use this for validating entity references in create/edit actions.
    /// </summary>
    protected async Task<bool> IsValidContactAsync(Guid id)
    {
        return id != Guid.Empty && await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }

    protected IActionResult RedirectToEntity(Guid id, string? type)
    {
        if (id == Guid.Empty || string.IsNullOrEmpty(type))
        {
            return RedirectToAction("Index", "Home");
        }

        if (type == EntityTypes.Person)
        {
            return RedirectToAction("Details", "Contacts", new { id });
        }
        // Add other types here
        return RedirectToAction("Index", "Home");
    }
}
