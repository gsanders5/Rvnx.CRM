using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;
using System.Linq.Expressions;

namespace Rvnx.CRM.Infrastructure.Repositories;

public class Repository(CRMDbContext context) : IRepository
{
    private readonly CRMDbContext _context = context;

    public IQueryable<T> QueryUnfiltered<T>() where T : BaseEntity
    {
        return _context.Set<T>().IgnoreQueryFilters();
    }

    public async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<T?> GetByIdWithIncludesAsync<T>(Guid id, params string[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().AsQueryable();

        foreach (string include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<T>> ListAsync<T>(int? skip = null, int? take = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().AsQueryable();

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>().Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default, params string[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().Where(predicate);

        foreach (string include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsNoTrackingAsync<T>(int? skip = null, int? take = null,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().AsNoTracking();

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsNoTrackingAsync<T>(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default, params string[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().AsNoTracking().Where(predicate);

        foreach (string include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<TDto>> ListProjectedAsync<T, TDto>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TDto>> selector, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TDto>> ListProjectedAsync<T, TDto, TKey>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TDto>> selector, Expression<Func<T, TKey>> orderBy, bool descending = false,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>()
            .AsNoTracking()
            .Where(predicate);

        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        return await query
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry =
            await _context.Set<T>().AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        List<T> entitiesList = entities.ToList();
        await _context.Set<T>().AddRangeAsync(entitiesList, cancellationToken);
        return entitiesList;
    }

    public Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        _context.Set<T>().Update(entity);
        return Task.FromResult(entity);
    }

    public Task<IEnumerable<T>> UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        List<T> entitiesList = entities.ToList();
        _context.Set<T>().UpdateRange(entitiesList);
        return Task.FromResult<IEnumerable<T>>(entitiesList);
    }

    public async Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        T? entity = _context.Set<T>().Local.FirstOrDefault(e => e.Id == id);
        entity ??= await _context.Set<T>().FindAsync([id], cancellationToken);

        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
        }
    }

    public Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : BaseEntity
    {
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            List<T> entities = await _context.Set<T>().Where(predicate).ToListAsync(cancellationToken);
            if (entities.Count != 0)
            {
                _context.Set<T>().RemoveRange(entities);
            }
        }
        else
        {
            await _context.Set<T>().Where(predicate).ExecuteDeleteAsync(cancellationToken);
        }
    }

    public Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : BaseEntity
    {
        _context.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new EntityConcurrencyException("A concurrency conflict occurred while saving changes.", ex);
        }
    }

    public async Task<bool> ExistsAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>().CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : BaseEntity
    {
        return await _context.Set<T>().CountAsync(predicate, cancellationToken);
    }
}
