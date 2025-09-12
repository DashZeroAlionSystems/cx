namespace CX.Container.Server.Domain.UiLogs.Services;

using CX.Container.Server.Domain.UiLogs;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IUiLogsRepository : IGenericRepository<UiLogs, Guid>
{
}

public sealed class UiLogsRepository : GenericRepository<UiLogs, Guid>, IUiLogsRepository
{
    private readonly AelaDbContext _dbContext;

    public UiLogsRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
