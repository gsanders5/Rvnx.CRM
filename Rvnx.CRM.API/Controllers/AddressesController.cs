using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text.Json;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages addresses for contacts. Each address has a type (e.g., "Home", "Work").
/// Requires entityId (contact GUID) when creating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressesController(IAddressService addressService) : ControllerBase
{
    private readonly IAddressService _addressService = addressService;

    /// <summary>
    /// List all addresses for a specific contact.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<AddressDto> addresses = await _addressService.GetByContactAsync(contactId);
        return Ok(addresses);
    }

    /// <summary>
    /// Create a new address. Required fields: line1, city, state, zip, country, addressType, entityId.
    /// </summary>
    /// <param name="model">The address data.</param>
    /// <returns>The new address's ID.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressFormDto model)
    {
        OperationResult result = await _addressService.CreateAsync(model);
        return result.ToCreatedResult();
    }

    /// <summary>
    /// Full update of an address. All fields are replaced.
    /// Use PATCH for partial updates.
    /// </summary>
    /// <param name="id">The address GUID.</param>
    /// <param name="model">The complete address data.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AddressFormDto model)
    {
        model.Id = id;
        OperationResult result = await _addressService.UpdateAsync(id, model);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Partial update of an address using JSON Merge Patch (RFC 7396).
    /// Only include the fields you want to change.
    /// </summary>
    /// <param name="id">The address GUID.</param>
    /// <param name="patch">A JSON object containing only the fields to update.</param>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] JsonElement patch)
    {
        AddressFormDto? existing = await _addressService.GetFormAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        IActionResult? validationFailure = JsonMergePatchHelper.ApplyAndValidate(existing, patch);
        if (validationFailure != null)
        {
            return validationFailure;
        }

        OperationResult result = await _addressService.UpdateAsync(id, existing);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete an address.
    /// </summary>
    /// <param name="id">The address GUID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _addressService.DeleteAsync(id);
        return result.ToNoContentResult();
    }
}
