using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages pets associated with contacts. Pets can be linked to multiple contacts via contactIds.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetsController(IPetService petService) : ControllerBase
{
    private readonly IPetService _petService = petService;

    /// <summary>
    /// List all pets for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<PetDto> pets = await _petService.GetByContactAsync(contactId);
        return Ok(pets);
    }

    /// <summary>
    /// Create a new pet. Required fields: name, entityId.
    /// The contactIds array links the pet to one or more contacts.
    /// </summary>
    /// <param name="model">The pet data.</param>
    /// <returns>The new pet's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PetFormDto model)
    {
        Core.Models.OperationResult result = await _petService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    /// <summary>
    /// Full update of a pet. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The pet GUID.</param>
    /// <param name="model">The complete pet data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PetFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _petService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Partial update of a pet using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The pet GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        PetFormDto? existing = await _petService.GetFormAsync(id);
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

        Core.Models.OperationResult result = await _petService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    /// <summary>
    /// Delete a pet.
    /// </summary>
    /// <param name="id">The pet GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _petService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
