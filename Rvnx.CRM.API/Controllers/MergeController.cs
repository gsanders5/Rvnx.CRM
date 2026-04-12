using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Merges two contacts into one. This is a destructive operation —
/// the source contact is permanently deleted after its data is merged into the target.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MergeController(IMergeService mergeService) : ControllerBase
{
    private readonly IMergeService _mergeService = mergeService;

    /// <summary>
    /// Merge the source contact into the target contact. All data from the source
    /// (notes, relationships, activities, etc.) is moved to the target, and the
    /// source contact is permanently deleted. This cannot be undone.
    /// </summary>
    /// <param name="targetContactId">The contact GUID to keep (receives merged data).</param>
    /// <param name="sourceContactId">The contact GUID to merge and delete.</param>
    [HttpPost]
    public async Task<IActionResult> Merge([FromQuery] Guid targetContactId, [FromQuery] Guid sourceContactId)
    {
        await _mergeService.MergeContactsAsync(targetContactId, sourceContactId);
        return NoContent();
    }
}
