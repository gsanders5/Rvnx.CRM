using Rvnx.CRM.Core.DTOs.Contact;
using System.Security.Claims;

namespace Rvnx.CRM.Core.Interfaces;

public interface ISelfContactService
{
    Task<Guid?> GetSelfContactIdAsync(ClaimsPrincipal user);
    Task<ContactFormDto?> GetSelfContactFormAsync(ClaimsPrincipal user);
    Task<ContactOperationResult> CreateSelfContactAsync(ClaimsPrincipal user, ContactFormDto contactDto);
}
