using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Sources.Dtos;
using CX.Container.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Sources.Services;

public interface ISourceRepository : IGenericRepository<Source, Guid>
{
    public Task<List<SourceCountDto>> GetTotalCountOverTimeAsync(CancellationToken cancellationToken = default);
}

public sealed class SourceRepository : GenericRepository<Source, Guid>, ISourceRepository
{
    private readonly AelaDbContext _dbContext;

    public SourceRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SourceCountDto>> GetTotalCountOverTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Source>()
            .AsNoTracking()
            .GroupBy(a => new { a.CreatedOn.Month, a.CreatedOn.Year })
            .Select(g => new SourceCountDto
            {
                Month = g.Key.Month,
                Year = g.Key.Year,
                TotalCount = g.Count(),
            }).ToListAsync(cancellationToken: cancellationToken);
    }
}