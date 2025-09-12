namespace CX.Container.Server.Domain.Citations.Services;

using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface ICitationRepository : IGenericRepository<Citation, Guid>
{
    IQueryable<Citation> GetBySourceDocumentId(Guid messageId);
}

public sealed class CitationRepository : GenericRepository<Citation, Guid>, ICitationRepository
{
    private readonly AelaDbContext _dbContext;

    public CitationRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Citation> GetBySourceDocumentId(Guid sourceDocumentId)
    {
        var query = from citation in _dbContext.Citations
            where citation.SourceDocumentId == sourceDocumentId && citation.IsDeleted == false
            orderby citation.CreatedOn
            select citation;

        return query;
    }
}
