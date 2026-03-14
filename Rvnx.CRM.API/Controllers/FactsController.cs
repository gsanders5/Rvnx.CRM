using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FactsController(IFactService factService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IFactService _factService = factService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        ContactDetailDto? contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        return contactDetails == null ? NotFound() : Ok(contactDetails.Facts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FactFormDto model)
    {
        Core.Models.OperationResult result = await _factService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FactFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _factService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _factService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}