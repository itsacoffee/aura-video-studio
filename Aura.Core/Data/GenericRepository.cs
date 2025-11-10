using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Data;

/// <summary>
/// Generic repository implementation for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public class GenericRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    protected readonly AuraDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ILogger<GenericRepository<TEntity, TKey>> _logger;

    public GenericRepository(AuraDbContext context, ILogger<GenericRepository<TEntity, TKey>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, ct);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.ToListAsync(ct);
    }

    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, ct);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _dbSet.AddRangeAsync(entities, ct);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync(ct);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        if (predicate == null)
        {
            return await _dbSet.CountAsync(ct);
        }
        return await _dbSet.CountAsync(predicate, ct);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }
}
