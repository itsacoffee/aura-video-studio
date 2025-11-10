using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Data;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get entity by primary key
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Find entities matching a predicate
    /// </summary>
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Get the first entity matching a predicate, or null
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Delete an entity
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Delete entities matching a predicate
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    /// <summary>
    /// Count entities matching a predicate
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>
    /// Check if any entities match a predicate
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
