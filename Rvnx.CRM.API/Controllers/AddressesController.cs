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
public class AddressesController(IAddressService addressService) : ControllerBase
{
    private readonly IAddressService _addressService = addressService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<AddressDto> addresses = await _addressService.GetByContactAsync(contactId);
        return Ok(addresses);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressFormDto model)
    {
        Core.Models.OperationResult result = await _addressService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AddressFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _addressService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        AddressFormDto? existing = await _addressService.GetFormAsync(id);
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

        Core.Models.OperationResult result = await _addressService.UpdateAsync(id, existing);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _addressService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
