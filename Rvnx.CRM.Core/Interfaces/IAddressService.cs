using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Interfaces;

public interface IAddressService
{
    Task<List<AddressDto>> GetByContactAsync(Guid contactId);
    Task<OperationResult> CreateAsync(AddressFormDto dto);
    Task<OperationResult> UpdateAsync(Guid id, AddressFormDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<AddressFormDto?> GetFormAsync(Guid id);
    Task<AddressFormDto?> GetFormForCreateAsync(Guid contactId);
    Task<Address?> GetByIdAsync(Guid id);
}
