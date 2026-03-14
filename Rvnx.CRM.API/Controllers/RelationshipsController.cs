using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelationshipsController(IRelationshipService relationshipService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IRelationshipService _relationshipService = relationshipService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        var contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        if (contactDetails == null) return NotFound();
        return Ok(contactDetails.Relationships);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        var result = await _relationshipService.CreateRelationshipAsync(model, selectedRelationshipType);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        var result = await _relationshipService.UpdateRelationshipAsync(id, model, selectedRelationshipType);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _relationshipService.DeleteRelationshipAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return NoContent();
    }
}
