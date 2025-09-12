namespace CX.Container.Server.Domain.Threads.Services;

using CX.Container.Server.Domain.Threads;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IThreadRepository : IGenericRepository<Thread, Guid>
{
}

public sealed class ThreadRepository : GenericRepository<Thread, Guid>, IThreadRepository
{
    private readonly AelaDbContext _dbContext;

    public ThreadRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
