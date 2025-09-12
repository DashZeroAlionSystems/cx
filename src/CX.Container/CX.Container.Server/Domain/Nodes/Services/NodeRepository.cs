namespace CX.Container.Server.Domain.Nodes.Services;

using CX.Container.Server.Domain.Nodes;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using Microsoft.EntityFrameworkCore;
using CX.Container.Server.Domain.Nodes.Dtos;
using CX.Container.Server.Domain.Dashboards.Dtos;

public interface INodeRepository : IGenericRepository<Node, Guid>
{
    Task<Node> GetRootByProjectId(Guid id, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<Node> GetByIdWithChildrenAsync(Guid id, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<List<Node>> GetNodesByParentIdAsync(Guid parentId, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<bool> HasAssetNodesByProjectId(Guid projectId, bool withTracking = false, CancellationToken cancellationToken = default);    
    Task<List<CategoryNodeDto>> GetCategoryNodesByProjectId(Guid projectId, bool withTracking = true, CancellationToken cancellationToken = default);
    Task<ProjectSummaryDto> GetProjectSummaryByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public class NodeRepository : GenericRepository<Node, Guid>, INodeRepository
{
    private readonly AelaDbContext _dbContext;

    public NodeRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Node> GetRootByProjectId(Guid id, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Node>().AsQueryable();

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var entity = await query.Include(n => n.Nodes).FirstOrDefaultAsync(x => x.ProjectId.Equals(id) && x.ParentId == null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException($"{typeof(Node).Name} with a project id '{id}' was not found.");
        }

        return entity;
    }

    public async Task<Node> GetByIdWithChildrenAsync(Guid id, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Node>().AsQueryable();

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }
                
        query = query.Include("Nodes");

        var entity = await query.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException($"{typeof(Node).Name} with an id '{id}' was not found.");
        }

        return entity;
    }

    public async Task<List<Node>> GetNodesByParentIdAsync(Guid parentId, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        var query = from node in _dbContext.Nodes
                    where node.ParentId == parentId
                    select node;

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var nodes = await query.ToListAsync(cancellationToken);

        return nodes;
    }
        
    public async Task<List<CategoryNodeDto>> GetCategoryNodesByProjectId(Guid projectId, bool withTracking = true, CancellationToken cancellationToken = default)
    {
        var query = from node in _dbContext.Nodes
                    where node.ProjectId == projectId
                    where node.IsAsset == false
                    where node.IsDeleted == false
                    select new CategoryNodeDto
                    {
                        Id = node.Id,
                        ProjectId = node.ProjectId,
                        ParentId = node.ParentId,
                        Name = node.Name,
                        IsAsset = node.IsAsset
                    };

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var nodes = await query.ToListAsync(cancellationToken);

        return nodes;
    }

    public async Task<bool> HasAssetNodesByProjectId(Guid projectId, bool withTracking = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Node>().AsQueryable();

        if (!withTracking)
        {
            query = query.AsNoTracking();
        }

        var hasNodes = await query.AnyAsync(x => x.ProjectId.Equals(projectId) && x.IsAsset, cancellationToken);

        return hasNodes;
    }

    public async Task<ProjectSummaryDto> GetProjectSummaryByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var summary = await _dbContext.Nodes
            .AsNoTracking()
            .Where(node => node.ProjectId == projectId && !node.IsDeleted)
            .GroupBy(node => true) // Group by a constant to ensure single group
            .Select(group => new
            {                
                TotalCategories = group.Count(node => node.IsAsset == false), // Sum 1 for categories                
                CategoriesWithAssets = group.Count(node => node.IsAsset == false && node.Nodes.Any(child => !child.IsDeleted && child.IsAsset)), // Sum 1 for categories with assets
                TotalAssets = group.Count(node => node.IsAsset) // Sum 1 for assets
            })
            .FirstAsync(cancellationToken);

        // Query to count trained source documents
        var doneSourceDocuments = await _dbContext.SourceDocuments
            .AsNoTracking()
            .Include(sd => sd.Node)            
            .Where(sd => sd.Node.ProjectId == projectId && 
                         (sd.Status.Value == SourceDocumentStatus.SourceDocumentStatus.TrainingDone() ||
                         sd.Status.Value == SourceDocumentStatus.SourceDocumentStatus.Done())
                         && !sd.IsDeleted && !sd.Node.IsDeleted)            
            .Select(sd => sd.NodeId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Query to count errored source documents
        var errorSourceDocuments = await _dbContext.SourceDocuments
            .AsNoTracking()
            .Include(sd => sd.Node)
            .Where(sd => sd.Node.ProjectId == projectId && sd.Status.Value == SourceDocumentStatus.SourceDocumentStatus.Error() && !sd.IsDeleted && !sd.Node.IsDeleted)
            .Select(sd => sd.NodeId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Query to count public bucket source documents (awaiting training)
        var publicBucketSourceDocuments = await _dbContext.SourceDocuments
            .AsNoTracking()
            .Include(sd => sd.Node)
            .Where(sd => sd.Node.ProjectId == projectId && sd.Status.Value == SourceDocumentStatus.SourceDocumentStatus.PublicBucket() && !sd.IsDeleted && !sd.Node.IsDeleted)
            .Select(sd => sd.NodeId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Query to count source documents that are neither 'Done', 'Error', nor 'PublicBucket' (in training)
        var processingSourceDocuments = await _dbContext.SourceDocuments
            .AsNoTracking()
            .Include(sd => sd.Node)
            .Where(sd => sd.Node.ProjectId == projectId 
                         && sd.Status.Value != SourceDocumentStatus.SourceDocumentStatus.Done()
                         && sd.Status.Value != SourceDocumentStatus.SourceDocumentStatus.Error()
                         && sd.Status.Value != SourceDocumentStatus.SourceDocumentStatus.PublicBucket()
                         && !sd.IsDeleted && !sd.Node.IsDeleted)
            .Select(sd => sd.NodeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var trainedFilePercentage = summary.TotalAssets > 0 ? (double)doneSourceDocuments / summary.TotalAssets * 100 : 0;
        var categoryCompletenessPercentage = summary.TotalCategories > 0 ? (double)summary.CategoriesWithAssets / summary.TotalCategories * 100 : 0;

        return new ProjectSummaryDto
        {            
            TotalCategories = summary.TotalCategories,
            TotalAssets = summary.TotalAssets,
            TotalNodes = summary.TotalCategories + summary.TotalAssets,
            CategoryCompletenessPercentage = (int)Math.Round(categoryCompletenessPercentage),
            TrainedAssetPercentage = (int)Math.Round(trainedFilePercentage),
            DoneSourceDocuments = doneSourceDocuments,
            ErrorSourceDocuments = errorSourceDocuments,
            PublicBucketSourceDocuments = publicBucketSourceDocuments,
            ProcessingBucketSourceDocuments = processingSourceDocuments            
        };
    }
}