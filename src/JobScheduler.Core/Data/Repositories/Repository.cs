#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace JobScheduler.Core.Data.Repositories;

/// <summary>
/// Generic repository implementation providing base CRUD operations.
/// Works with any entity type inheriting from the base class.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly JobSchedulerContext _context;
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{T}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public Repository(JobSchedulerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Asynchronously gets an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Asynchronously gets all entities.
    /// </summary>
    /// <returns>A collection containing all entities.</returns>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Asynchronously finds entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>A collection of entities that match the predicate.</returns>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Asynchronously returns the first entity matching the specified predicate, or default if none found.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>The first entity matching the predicate; otherwise, null.</returns>
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Asynchronously counts the number of entities, optionally filtered by a predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities (optional).</param>
    /// <returns>The number of entities.</returns>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate is null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    /// <summary>
    /// Asynchronously adds an entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddAsync(T entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity);
    }

    /// <summary>
    /// Asynchronously adds a collection of entities to the repository.
    /// </summary>
    /// <param name="entities">The collection of entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        if (entities is null)
            throw new ArgumentNullException(nameof(entities));

        await _dbSet.AddRangeAsync(entities);
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public virtual void Update(T entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
    }

    /// <summary>
    /// Updates a collection of entities in the repository.
    /// </summary>
    /// <param name="entities">The collection of entities to update.</param>
    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        if (entities is null)
            throw new ArgumentNullException(nameof(entities));

        _dbSet.UpdateRange(entities);
    }

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public virtual void Remove(T entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Removes a collection of entities from the repository.
    /// </summary>
    /// <param name="entities">The collection of entities to remove.</param>
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        if (entities is null)
            throw new ArgumentNullException(nameof(entities));

        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Asynchronously determines whether any entity matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition to test entities.</param>
    /// <returns>True if any entity matches the predicate; otherwise, false.</returns>
    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    /// <summary>
    /// Asynchronously saves all changes made in the repository to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}