using CX.Container.Server.Databases;

namespace CX.Container.Server.Services;

public interface IGenericQueryRepository<TEntity> : IAelaServerScopedService
{
    IQueryable<TEntity> Query();
}

public abstract class GenericQueryRepository<TEntity> : IGenericQueryRepository<TEntity>
    where TEntity : class
{
    private readonly AelaDbReadContext _dbContext;

    protected GenericQueryRepository(AelaDbReadContext dbContext)
    {
        this._dbContext = dbContext;
    }
    
    public virtual IQueryable<TEntity> Query()
    {
        return _dbContext.Set<TEntity>();
    }
}