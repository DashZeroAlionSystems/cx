namespace CX.Container.Server.Services;

using Databases;

public interface IUnitOfWork : IAelaServerScopedService
{
    Task<int> CommitChanges(CancellationToken cancellationToken = default);
    Task WrapInTransactionAsync(Func<AelaDbContext, CancellationToken, Task> dbUpdates, CancellationToken cancellationToken = default);
    
}

public sealed class UnitOfWork(AelaDbContext dbContext) : IUnitOfWork
{
    public async Task<int> CommitChanges(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task WrapInTransactionAsync(Func<AelaDbContext, CancellationToken, Task> dbUpdates, CancellationToken cancellationToken = default)
    {
        return dbContext.WrapInTransactionAsync(dbUpdates, cancellationToken);
    }
}
