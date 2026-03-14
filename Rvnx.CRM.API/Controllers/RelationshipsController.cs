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
        ContactDetailDto? contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        return contactDetails == null ? NotFound() : Ok(contactDetails.Relationships);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        RelationshipOperationResult result = await _relationshipService.CreateRelationshipAsync(model, selectedRelationshipType);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Relationship model, [FromQuery] string selectedRelationshipType)
    {
        RelationshipOperationResult result = await _relationshipService.UpdateRelationshipAsync(id, model, selectedRelationshipType);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Core.Models.OperationResult result = await _relationshipService.DeleteRelationshipAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}