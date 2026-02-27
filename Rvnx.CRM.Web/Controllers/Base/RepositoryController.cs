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

    protected async Task<string> GetEntityName(Guid id, string type)
    {
        if (type == EntityTypes.Person)
        {
            List<string> names = await _repository.ListProjectedAsync<Contact, string>(
                c => c.Id == id,
                c => c.FirstName + " " + (c.LastName ?? ""));
            return names.FirstOrDefault()?.Trim() ?? "Unknown Person";
        }
        else if (type == EntityTypes.Company)
        {
            List<string> names = await _repository.ListProjectedAsync<Employer, string>(
                c => c.Id == id,
                c => c.CompanyName);
            return names.FirstOrDefault() ?? "Unknown Company";
        }
        return "Unknown Entity";
    }

    protected async Task<bool> IsPartialContactAsync(Guid id)
    {
        List<bool> partials = await _repository.ListProjectedAsync<Contact, bool>(
            c => c.Id == id,
            c => c.IsPartial);
        return partials.FirstOrDefault();
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
