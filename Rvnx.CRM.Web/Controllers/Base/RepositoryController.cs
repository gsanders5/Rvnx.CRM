using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

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

    protected IActionResult RedirectToEntity(Guid id, EntityType? type)
    {
        return id == Guid.Empty || type == null
            ? RedirectToAction("Index", "Home")
            : type == EntityType.Person ? RedirectToAction("Details", "Contacts", new { id }) : RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Maps a service <see cref="OperationResult"/> to the standard controller response:
    /// success redirects to the associated entity, IsNotFound returns 404, otherwise returns null
    /// (signalling the caller should re-render the form). Eliminates repeated boilerplate across
    /// every Create/Edit/Delete POST action.
    /// </summary>
    protected IActionResult? HandleOperationResult(OperationResult result)
    {
        if (result.Success)
        {
            return RedirectToEntity(result.RedirectId, result.RedirectType);
        }

        return result.IsNotFound ? NotFound() : null;
    }

    protected async Task PopulateContactsSelectList(IContactReadService contactReadService, List<Guid> selectedIds)
    {
        List<(Guid Id, string FullName)> contacts = await contactReadService.GetContactNamesAsync();
        ViewBag.ContactsList = new MultiSelectList(
            contacts.OrderBy(c => c.FullName).Select(c => new { c.Id, c.FullName }),
            "Id",
            "FullName",
            selectedIds);
    }
}
