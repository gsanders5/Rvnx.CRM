using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AttachmentsController(IAttachmentService attachmentService) : ControllerBase
{
    private readonly IAttachmentService _attachmentService = attachmentService;

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid id)
    {
        var attachment = await _attachmentService.GetAttachmentAsync(id);

        if (attachment == null)
        {
            return NotFound();
        }

        var contentDto = await _attachmentService.GetAttachmentContentAsync(id);

        if (contentDto == null)
        {
            return NotFound();
        }

        return File(contentDto.Content, contentDto.ContentType, attachment.FileName);
    }
}
