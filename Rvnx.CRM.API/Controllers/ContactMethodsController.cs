using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactMethodsController(IContactMethodService contactMethodService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IContactMethodService _contactMethodService = contactMethodService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        var contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        if (contactDetails == null) return NotFound();
        return Ok(contactDetails.ContactMethods);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactMethodFormDto model)
    {
        var result = await _contactMethodService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactMethodFormDto model)
    {
        model.Id = id;
        var result = await _contactMethodService.UpdateAsync(id, model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _contactMethodService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }
}
