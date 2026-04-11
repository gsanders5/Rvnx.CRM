using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController(INoteService noteService) : ControllerBase
{
    private readonly INoteService _noteService = noteService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<Core.DTOs.Base.NoteDto> notes = await _noteService.GetByContactAsync(contactId);
        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NoteFormViewModel model)
    {
        Core.Models.OperationResult result = await _noteService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NoteFormViewModel model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _noteService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        NoteFormViewModel? existing = await _noteService.GetFormAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        JsonMergePatchHelper.ApplyPatch(existing, patch);

        List<string> errors = JsonMergePatchHelper.Validate(existing);
        if (errors.Count > 0)
        {
            return BadRequest(new { Errors = errors });
        }

        Core.Models.OperationResult result = await _noteService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _noteService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}