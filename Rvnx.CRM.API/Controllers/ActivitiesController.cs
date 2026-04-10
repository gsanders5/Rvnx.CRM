using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController(IActivityService activityService) : ControllerBase
{
    private readonly IActivityService _activityService = activityService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ActivityDto> activities = await _activityService.GetByContactAsync(contactId);
        return Ok(activities);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ActivityFormDto model)
    {
        Core.Models.OperationResult result = await _activityService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActivityFormDto model)
    {
        model.Id = id;
        Core.Models.OperationResult result = await _activityService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _activityService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}
