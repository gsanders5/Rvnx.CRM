using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ContactsController(IContactReadService contactReadService, IContactManagementService contactManagementService) : ControllerBase
{
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;

    [HttpGet]
    public async Task<IActionResult> GetContacts([FromQuery] bool includePartial = false)
    {
        var contacts = await _contactReadService.GetIndexDataAsync(includePartial);
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContact(Guid id)
    {
        var contact = await _contactReadService.GetContactDetailsAsync(id);
        if (contact == null)
        {
            return NotFound();
        }

        return Ok(contact);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContact([FromBody] ContactFormDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _contactManagementService.CreateContactAsync(model);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetContact), new { id = result.ContactId }, result.ContactId);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] ContactFormDto model)
    {
        if (id != model.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _contactManagementService.UpdateContactAsync(id, model, null, null, null);

        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContact(Guid id)
    {
        await _contactManagementService.DeleteContactAsync(id);
        return NoContent();
    }
}
