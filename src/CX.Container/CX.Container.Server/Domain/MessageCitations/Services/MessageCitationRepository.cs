namespace CX.Container.Server.Domain.MessageCitations.Services;

using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IMessageCitationRepository : IGenericRepository<MessageCitation, Guid>
{
    IQueryable<MessageCitation> GetByMessageId(Guid messageId);
}

public sealed class MessageCitationRepository : GenericRepository<MessageCitation, Guid>, IMessageCitationRepository
{
    private readonly AelaDbContext _dbContext;

    public MessageCitationRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<MessageCitation> GetByMessageId(Guid messageId)
    {
        var query = from citation in _dbContext.MessageCitations
            where citation.MessageId == messageId && citation.IsDeleted == false
            orderby citation.CreatedOn
            select citation;

        return query;
    }
}
