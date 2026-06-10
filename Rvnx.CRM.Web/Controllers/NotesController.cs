using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class NotesController(INoteService noteService, IRepository repository, IContactLookupService contactLookupService)
    : RepositoryController(repository)
{
    private readonly INoteService _noteService = noteService;
    private readonly IContactLookupService _contactLookupService = contactLookupService;

    [HttpGet]
    public async Task<IActionResult> Create(Guid contactId)
    {
        NoteFormViewModel? viewModel = await _noteService.GetFormForCreateAsync(contactId);
        return viewModel == null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NoteFormViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            OperationResult result = await _noteService.CreateAsync(viewModel);
            return result.Success
                ? RedirectToContact(result.RedirectId)
                : result.IsNotFound ? NotFound() : BadRequest(result.ErrorMessage);
        }

        if (viewModel.ContactId != Guid.Empty)
        {
            viewModel.ContactName = await _contactLookupService.GetContactNameAsync(viewModel.ContactId);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        NoteFormViewModel? viewModel = await _noteService.GetFormAsync(id.Value);
        return viewModel == null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, NoteFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            IActionResult? handled = HandleOperationResult(await _noteService.UpdateAsync(id, viewModel));
            if (handled != null)
            {
                return handled;
            }
        }

        if (viewModel.ContactId != Guid.Empty)
        {
            viewModel.ContactName = await _contactLookupService.GetContactNameAsync(viewModel.ContactId);
        }

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        OperationResult result = await _noteService.DeleteAsync(id);
        return result.Success
            ? RedirectToContact(result.RedirectId)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFavorite(Guid id)
    {
        OperationResult result = await _noteService.ToggleFavoriteAsync(id);
        return HandleOperationResult(result) ?? RedirectToAction("Index", "Home");
    }
}
