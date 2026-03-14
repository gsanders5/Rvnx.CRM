using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignificantDatesController(ISignificantDateService significantDateService) : ControllerBase
{
    private readonly ISignificantDateService _significantDateService = significantDateService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        var dates = await _significantDateService.GetByContactAsync(contactId);
        return Ok(dates);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SignificantDateDto model)
    {
        var result = await _significantDateService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SignificantDateDto model)
    {
        model.Id = id;
        var result = await _significantDateService.UpdateAsync(id, model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _significantDateService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }
}
