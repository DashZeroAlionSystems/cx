namespace CX.Container.Server.Services;

using CX.Container.Server.Domain;
using CX.Container.Server.Databases;
using CX.Container.Server.Exceptions;
using Microsoft.EntityFrameworkCore;

public interface IGenericRepository<TEntity, TKey> : IAelaServerScopedService
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdOrDefault(TKey id, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<TEntity> GetById(TKey id, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<bool> Exists(TKey id, CancellationToken cancellationToken = default);
    Task Add(TEntity entity, CancellationToken cancellationToken = default);    
    Task AddRange(IEnumerable<TEntity> entity, CancellationToken cancellationToken = default);    
    void Update(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entity);
}

public abstract class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> 
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly AelaDbContext _dbContext;

    protected GenericRepository(AelaDbContext dbContext)
    {
        this._dbContext = dbContext;
    }
    
    public virtual IQueryable<TEntity> Query()
    {
        return _dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdOrDefault(TKey id, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        return withTracking 
            ? await _dbContext.Set<TEntity>()
                .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken) 
            : await _dbContext.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<TEntity> GetById(TKey id, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdOrDefault(id, withTracking, cancellationToken);
        
        if(entity == null)
            throw new NotFoundException($"{typeof(TEntity).Name} with an id '{id}' was not found.");

        return entity;
    }

    public virtual async Task<bool> Exists(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task Add(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbContext.Set<TEntity>().RemoveRange(entities);
    }
}
