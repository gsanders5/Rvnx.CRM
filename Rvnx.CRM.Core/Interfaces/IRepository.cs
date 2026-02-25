using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRepository
{
    // Read Operations
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<T?> GetByIdWithIncludesAsync<T>(Guid id, params string[] includes) where T : BaseEntity;

    Task<List<T>> ListAsync<T>(int? skip = null, int? take = null, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params string[] includes) where T : BaseEntity;

    Task<List<T>> ListAsNoTrackingAsync<T>(int? skip = null, int? take = null, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<List<T>> ListAsNoTrackingAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params string[] includes) where T : BaseEntity;

    Task<List<TDto>> ListProjectedAsync<T, TDto>(Expression<Func<T, bool>> predicate, Expression<Func<T, TDto>> selector, CancellationToken cancellationToken = default) where T : BaseEntity;


    // Create Operations  
    Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : BaseEntity;


    // Update Operations
    Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;


    // Delete Operations
    Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : BaseEntity;


    // Persistence
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);


    // Utility
    Task<bool> ExistsAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : BaseEntity;

    Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : BaseEntity;
}