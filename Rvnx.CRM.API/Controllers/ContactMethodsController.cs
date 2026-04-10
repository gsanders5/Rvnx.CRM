using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactMethodsController(IContactMethodService contactMethodService) : ControllerBase
{
    private readonly IContactMethodService _contactMethodService = contactMethodService;

    [HttpGet("contact/{contactId}")]
    public async Task<IActionResult> ListByContact(Guid contactId)
    {
        List<ContactMethodDto> methods = await _contactMethodService.GetByContactAsync(contactId);
        return Ok(methods);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactMethodFormDto model)
    {
        OperationResult result = await _contactMethodService.CreateAsync(model);
        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }
        return Ok(new { Id = result.RedirectId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ContactMethodFormDto model)
    {
        model.Id = id;
        OperationResult result = await _contactMethodService.UpdateAsync(id, model);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        OperationResult result = await _contactMethodService.DeleteAsync(id);
        return !result.Success ? BadRequest(new { Error = result.ErrorMessage }) : NoContent();
    }
}