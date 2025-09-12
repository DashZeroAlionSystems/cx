using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Dashboards.Dtos;
using CX.Container.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.SourceDocuments.Services;

public interface ISourceDocumentRepository : IGenericRepository<SourceDocument, Guid>
{
    Task<SourceDocument> GetByNodeId(Guid nodeId, bool withTracking = true,
        CancellationToken cancellationToken = default);
    Task<SourceDocument> GetWithCitations(Guid id, CancellationToken cancellationToken = default);
    Task<ClientSummaryDto> GetClientSummary(CancellationToken cancellationToken = default);
    Task<List<DocumentCountDto>> GetTotalCountOverTimeAsync(CancellationToken cancellationToken = default);
}

public sealed class SourceDocumentRepository : GenericRepository<SourceDocument, Guid>, ISourceDocumentRepository
{
    private readonly AelaDbContext _dbContext;

    public SourceDocumentRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SourceDocument> GetByNodeId(Guid id, bool withTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<SourceDocument>().AsQueryable();

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var entity = await query.Include(sd => sd.Citations)
            .FirstOrDefaultAsync(x => x.NodeId.Equals(id), cancellationToken);

        return entity;
    }

    public async Task<SourceDocument> GetWithCitations(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<SourceDocument>().AsQueryable().Include(x => x.Citations);
        var entity = await query.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

        return entity;
    }

    public async Task<ClientSummaryDto> GetClientSummary(CancellationToken cancellationToken = default)
    {
        var summary = await _dbContext.Set<SourceDocument>()
            .AsNoTracking()
            .GroupBy(document => document.Status.Value)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var clientSummaryDto = new ClientSummaryDto
        {
            TotalDocuments = summary.Sum(pair => pair.Value),
            TotalDoneDocuments = summary.GetValueOrDefault("Done"),
            TotalTrainingDoneDocuments = summary.GetValueOrDefault("TrainingDone"),
            TotalErrorDocuments = summary.GetValueOrDefault("Error"),
            TotalDecoratingDocuments = summary.GetValueOrDefault("Decorating"),
            TotalDecoratingDoneDocuments = summary.GetValueOrDefault("DecoratingDone"),
            TotalQueuedForRetrainDocuments = summary.GetValueOrDefault("QueuedForRetrain"),
            TotalPublicBucketDocuments = summary.GetValueOrDefault("PublicBucket"),
            TotalPrivateBucketDocuments = summary.GetValueOrDefault("PrivateBucket"),
            TotalOCRDocuments = summary.GetValueOrDefault("OCR")
        };

        return clientSummaryDto;
    }

    public async Task<List<DocumentCountDto>> GetTotalCountOverTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<SourceDocument>()
            .AsNoTracking()
            .GroupBy(a => new { a.CreatedOn.Month, a.CreatedOn.Year })
            .Select(g => new DocumentCountDto
            {
                Month = g.Key.Month,
                Year = g.Key.Year,
                TotalCount = g.Count(),
            })
            .OrderBy(a => a.Year)
            .ThenBy(a => a.Month)
            .ToListAsync();
    }
}