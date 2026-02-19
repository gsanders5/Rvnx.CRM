namespace Rvnx.CRM.Core.Interfaces;

public interface IEntityService
{
    Task<bool> ExistsAsync(string entityType, Guid id);
}
