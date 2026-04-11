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
public class PetsController(IPetService petService) : ControllerBase
{
    private readonly IPetService _petService = petService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<PetDto> pets = await _petService.GetByContactAsync(contactId);
        return Ok(pets);
    }

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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PetFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _petService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _petService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}