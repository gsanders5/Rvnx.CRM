using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController(
    IContactReadService contactReadService,
    IContactManagementService contactManagementService) : ControllerBase
{
    private readonly IContactReadService _contactReadService = contactReadService;
    private readonly IContactManagementService _contactManagementService = contactManagementService;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var contacts = await _contactReadService.GetIndexDataAsync(false);
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var contact = await _contactReadService.GetContactFormAsync(id);
        if (contact == null)
        {
            return NotFound();
        }
        return Ok(contact);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactFormDto model)
    {
        var result = await _contactManagementService.CreateContactAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { result.Errors });
        }
        return CreatedAtAction(nameof(Get), new { id = result.ContactId }, new { Id = result.ContactId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactFormDto model)
    {
        model.Id = id;
        var result = await _contactManagementService.UpdateContactAsync(id, model, null, null, null);
        if (!result.Success)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }
            return BadRequest(new { result.Errors });
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _contactManagementService.DeleteContactAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/photo/{attachmentId}")]
    public async Task<IActionResult> SetPhoto(Guid id, Guid attachmentId)
    {
        var result = await _contactManagementService.SetAttachmentAsProfilePhotoAsync(id, attachmentId);
        if (!result.Success)
        {
            if (result.IsNotFound) return NotFound();
            return BadRequest(new { result.Errors });
        }
        return NoContent();
    }

    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> UnsetPhoto(Guid id)
    {
        var result = await _contactManagementService.UnsetProfilePhotoAsync(id);
        if (!result.Success)
        {
            if (result.IsNotFound) return NotFound();
            return BadRequest(new { result.Errors });
        }
        return NoContent();
    }
}
