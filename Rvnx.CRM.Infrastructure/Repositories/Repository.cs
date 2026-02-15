using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Infrastructure.Data;

namespace Rvnx.CRM.Infrastructure.Repositories;

public class Repository(CRMDbContext context) : IRepository
{
    private readonly CRMDbContext _context = context;

    // Read Operations
    public async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<T>> ListAsync<T>(CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        return await _context.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task<List<T>> ListAsNoTrackingAsync<T>(CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        return await _context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    // Create Operations
    public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        var entry = await _context.Set<T>().AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public async Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        var entitiesList = entities.ToList();
        await _context.Set<T>().AddRangeAsync(entitiesList, cancellationToken);
        return entitiesList;
    }

    // Update Operations
    public Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        _context.Set<T>().Update(entity);
        return Task.FromResult(entity);
    }

    // Delete Operations
    public Task DeleteAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        var entity = _context.Set<T>().Local.FirstOrDefault(e => e.Id == id);
        if (entity == null)
        {
            entity = (T)Activator.CreateInstance(typeof(T), true)!;
            entity.Id = id;
        }

        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        _context.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    // Persistence
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    // Utility
    public async Task<bool> ExistsAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        return await _context.Set<T>().AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : CRMBaseEntity
    {
        return await _context.Set<T>().CountAsync(cancellationToken);
    }
}