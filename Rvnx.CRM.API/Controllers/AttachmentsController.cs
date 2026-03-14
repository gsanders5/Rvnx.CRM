using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService, IContactReadService contactReadService) : ControllerBase
{
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        var contactDetails = await _contactReadService.GetContactDetailsAsync(contactId);
        if (contactDetails == null) return NotFound();
        return Ok(contactDetails.Attachments);
    }

    [HttpPost("contact/{contactId}")]
    public async Task<IActionResult> Upload(Guid contactId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var content = stream.ToArray();

        var result = await _attachmentService.UploadAttachmentAsync(contactId, "Person", content, file.FileName);

        if (!result.Success)
        {
            if (result.IsNotFound) return NotFound();
            return BadRequest(new { result.Errors });
        }

        return Ok(new { Id = result.AttachmentId });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var attachment = await _attachmentService.GetAttachmentAsync(id);
        if (attachment == null) return NotFound();

        var content = await _attachmentService.GetAttachmentContentAsync(id);
        if (content == null) return NotFound();

        return File(content.Content, content.ContentType, attachment.FileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _attachmentService.DeleteAttachmentAsync(id);
        if (!result.Success)
        {
            if (result.IsNotFound) return NotFound();
            return BadRequest(new { result.Errors });
        }
        return NoContent();
    }
}
