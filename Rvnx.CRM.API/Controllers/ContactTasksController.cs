using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages to-do tasks associated with contacts. Tasks have a due date and completion status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactTasksController(IContactTaskService contactTaskService) : ControllerBase
{
    private readonly IContactTaskService _contactTaskService = contactTaskService;

    /// <summary>
    /// List all tasks for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ContactTaskDto> tasks = await _contactTaskService.GetByContactAsync(contactId);
        return Ok(tasks);
    }

    /// <summary>
    /// Create a new task. Required fields: title, dueDate, contactId.
    /// </summary>
    /// <param name="model">The task data.</param>
    /// <returns>The new task's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactTaskFormDto model)
    {
        OperationResult result = await _contactTaskService.CreateAsync(model);
        return result.ToCreatedResult();
    }

    /// <summary>
    /// Full update of a task. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The task GUID.</param>
    /// <param name="model">The complete task data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactTaskFormDto model)
    {
        model.Id = id;
        OperationResult result = await _contactTaskService.UpdateAsync(id, model);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Partial update of a task using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The task GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        ContactTaskFormDto? existing = await _contactTaskService.GetFormAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        IActionResult? validationFailure = JsonMergePatchHelper.ApplyAndValidate(existing, patch);
        if (validationFailure != null)
        {
            return validationFailure;
        }

        OperationResult result = await _contactTaskService.UpdateAsync(id, existing);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete a task.
    /// </summary>
    /// <param name="id">The task GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _contactTaskService.DeleteAsync(id);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Toggle a task's completion status. If completed, marks it incomplete; if incomplete, marks it completed.
    /// </summary>
    /// <param name="id">The task GUID.</param>
    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> ToggleComplete(Guid id)
    {
        OperationResult result = await _contactTaskService.ToggleCompleteAsync(id);
        return result.ToNoContentResult();
    }
}
