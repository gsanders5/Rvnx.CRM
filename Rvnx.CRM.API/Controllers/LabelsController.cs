using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

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
        List<LabelDto> labels = await _labelService.GetAllAsync();
        return Ok(labels);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LabelFormDto model)
    {
        LabelOperationResult result = await _labelService.CreateAsync(model.Name, model.Color);
        if (!result.Success)
        {
            return BadRequest(new { result.Errors });
        }
        return Ok(new { Id = result.LabelId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LabelFormDto model)
    {
        LabelOperationResult result = await _labelService.UpdateAsync(id, model.Name, model.Color);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _labelService.DeleteAsync(id);
        return NoContent();
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