using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRepository
{
    // Read Operations
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task<T?> GetByIdWithIncludesAsync<T>(Guid id, params string[] includes) where T : CRMBaseEntity;

    IQueryable<T> GetQuery<T>() where T : CRMBaseEntity;
    IQueryable<T> GetQueryAsNoTracking<T>() where T : CRMBaseEntity;

    Task<List<T>> ListAsync<T>(int? skip = null, int? take = null, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task<List<T>> ListAsNoTrackingAsync<T>(int? skip = null, int? take = null, CancellationToken cancellationToken = default) where T : CRMBaseEntity;

    // Create Operations  
    Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : CRMBaseEntity;

    // Update Operations
    Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity;

    // Delete Operations
    Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : CRMBaseEntity;

    // Persistence
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Utility
    Task<bool> ExistsAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity;
    Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : CRMBaseEntity;
}