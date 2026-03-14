using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MergeController(IMergeService mergeService) : ControllerBase
{
    private readonly IMergeService _mergeService = mergeService;

    [HttpPost]
    public async Task<IActionResult> Merge([FromQuery] Guid targetContactId, [FromQuery] Guid sourceContactId)
    {
        await _mergeService.MergeContactsAsync(targetContactId, sourceContactId);
        return NoContent();
    }
}