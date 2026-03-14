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
        List<ContactDto> contacts = await _contactReadService.GetIndexDataAsync(false);
        return Ok(contacts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        ContactFormDto? contact = await _contactReadService.GetContactFormAsync(id);
        return contact == null ? NotFound() : Ok(contact);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactFormDto model)
    {
        ContactOperationResult result = await _contactManagementService.CreateContactAsync(model);
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
        ContactOperationResult result = await _contactManagementService.UpdateContactAsync(id, model, null, null, null);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
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
        ContactOperationResult result = await _contactManagementService.SetAttachmentAsProfilePhotoAsync(id, attachmentId);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }

    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> UnsetPhoto(Guid id)
    {
        ContactOperationResult result = await _contactManagementService.UnsetProfilePhotoAsync(id);
        return !result.Success ? result.IsNotFound ? NotFound() : BadRequest(new { result.Errors }) : NoContent();
    }
}