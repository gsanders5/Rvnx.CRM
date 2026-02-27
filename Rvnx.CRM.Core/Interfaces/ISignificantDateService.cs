using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Interfaces;

public interface ISignificantDateService
{
    Task<OperationResult> CreateAsync(SignificantDateDto dto);
    Task<OperationResult> UpdateAsync(Guid id, SignificantDateDto dto);
    Task<OperationResult> DeleteAsync(Guid id);
    Task<SignificantDateDto?> GetDtoAsync(Guid id);
    Task<SignificantDate?> GetByIdAsync(Guid id);
}
