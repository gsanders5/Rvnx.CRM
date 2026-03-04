using Rvnx.CRM.Core.Models.Base;
using System.Linq.Expressions;

namespace Rvnx.CRM.Core.Interfaces;

public interface IRepository
{
    /// <summary>
    /// Provides an unfiltered IQueryable for the entity, bypassing Global Query Filters.
    /// Used for administrative, system-level, or cross-tenant operations.
    /// </summary>
    IQueryable<T> QueryUnfiltered<T>() where T : BaseEntity;

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Retrieves an entity by its ID, eagerly loading specified navigation properties.
    /// </summary>
    Task<T?> GetByIdWithIncludesAsync<T>(Guid id, params string[] includes) where T : BaseEntity;

    /// <summary>
    /// Retrieves a paged list of entities.
    /// </summary>
    Task<List<T>> ListAsync<T>(int? skip = null, int? take = null, CancellationToken cancellationToken = default)
        where T : BaseEntity;

    /// <summary>
    /// Retrieves a list of entities matching a predicate.
    /// </summary>
    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : BaseEntity;

    /// <summary>
    /// Retrieves the first entity matching a predicate, or null if no such entity exists.
    /// </summary>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : BaseEntity;

    /// <summary>
    /// Retrieves the first entity matching a predicate without tracking changes, eagerly loading specified navigation properties.
    /// </summary>
    Task<T?> FirstOrDefaultAsNoTrackingAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params string[] includes)
        where T : BaseEntity;

    /// <summary>
    /// Retrieves a list of entities matching a predicate, eagerly loading specified navigation properties.
    /// </summary>
    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default,
        params string[] includes) where T : BaseEntity;

    /// <summary>
    /// Retrieves a paged list of entities without tracking changes.
    /// </summary>
    Task<List<T>> ListAsNoTrackingAsync<T>(int? skip = null, int? take = null,
        CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Retrieves a list of entities matching a predicate without tracking changes.
    /// </summary>
    Task<List<T>> ListAsNoTrackingAsync<T>(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default, params string[] includes) where T : BaseEntity;

    /// <summary>
    /// Projects entities to a DTO matching a predicate.
    /// </summary>
    Task<List<TDto>> ListProjectedAsync<T, TDto>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TDto>> selector, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Projects the first entity matching a predicate to a DTO, or returns null if no entity matches.
    /// </summary>
    Task<TDto?> FirstOrDefaultProjectedAsync<T, TDto>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TDto>> selector, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Projects entities to a DTO matching a predicate, with ordering.
    /// </summary>
    Task<List<TDto>> ListProjectedAsync<T, TDto, TKey>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TDto>> selector, Expression<Func<T, TKey>> orderBy, bool descending = false,
        CancellationToken cancellationToken = default) where T : BaseEntity;


    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Adds a range of new entities.
    /// </summary>
    Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : BaseEntity;


    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;


    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Deletes the specified entity instance.
    /// </summary>
    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Deletes entities matching a predicate (bulk delete).
    /// </summary>
    Task DeleteAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : BaseEntity;

    /// <summary>
    /// Deletes a range of entities.
    /// </summary>
    Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : BaseEntity;


    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Checks if an entity with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Returns the total count of entities of type T.
    /// </summary>
    Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : BaseEntity;

    /// <summary>
    /// Returns the count of entities matching a predicate.
    /// </summary>
    Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : BaseEntity;
}
