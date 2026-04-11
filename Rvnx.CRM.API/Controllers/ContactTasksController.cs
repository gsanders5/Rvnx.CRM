using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactTasksController(IContactTaskService contactTaskService) : ControllerBase
{
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ContactTaskDto> tasks = await _contactTaskService.GetByContactAsync(contactId);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactTaskFormDto model)
    {
        Core.Models.OperationResult result = await _contactTaskService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactTaskFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _contactTaskService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        ContactTaskFormDto? existing = await _contactTaskService.GetFormAsync(id);
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

        Core.Models.OperationResult result = await _contactTaskService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _contactTaskService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> ToggleComplete(Guid id)
    {
        Core.Models.OperationResult result = await _contactTaskService.ToggleCompleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
