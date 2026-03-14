using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetsController(IPetService petService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IPetService _petService = petService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        var contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        if (contactDetails == null) return NotFound();
        return Ok(contactDetails.Pets);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PetFormDto model)
    {
        var result = await _petService.CreateAsync(model);
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
        var result = await _petService.UpdateAsync(id, model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _petService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }
}
