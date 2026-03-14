using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabelsController(ILabelService labelService) : ControllerBase
{
    private readonly ILabelService _labelService = labelService;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var labels = await _labelService.GetAllAsync();
        return Ok(labels);
    }

    [HttpPost("contact/{contactId}/{labelId}")]
    public async Task<IActionResult> Associate(Guid contactId, Guid labelId)
    {
        await _labelService.AssignLabelAsync(contactId, labelId);
        return NoContent();
    }

    [HttpDelete("contact/{contactId}/{labelId}")]
    public async Task<IActionResult> Disassociate(Guid contactId, Guid labelId)
    {
        await _labelService.RemoveLabelAsync(contactId, labelId);
        return NoContent();
    }
}
