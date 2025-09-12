using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Messages.Services;

public interface IMessageRepository : IGenericRepository<Message, Guid>
{
    IQueryable<Message> GetByThreadId(Guid threadId);
    IQueryable<MessageCountDto> GetMessageSummary();
    IQueryable<MessageRatingCountDto> GetMessageRatingSummary();
}

public sealed class MessageRepository : GenericRepository<Message, Guid>, IMessageRepository
{
    private readonly AelaDbContext _dbContext;

    public MessageRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Message> GetByThreadId(Guid threadId)
    {
        var query = from message in _dbContext.Messages
            where message.ThreadId == threadId && message.IsDeleted == false
            orderby message.CreatedOn
            select message;

        return query;
    }

    public IQueryable<MessageRatingCountDto> GetMessageRatingSummary()
    {
        var currentDate = DateTime.UtcNow.Date.AddDays(-7);

        return from message in _dbContext.Messages
            where message.CreatedOn >= currentDate && message.MessageType == "System"
            group message by message.Feedback
            into feedbackGroup
            select new MessageRatingCountDto(feedbackGroup.Key, feedbackGroup.Count());
    }

    public IQueryable<MessageCountDto> GetMessageSummary()
    {
        return _dbContext
            .Set<Message>()
            .AsNoTracking()
            .Where(x => x.MessageType == "User")
            .GroupBy(a => new { a.CreatedOn.Month, a.CreatedOn.Year })
            .Select(g => new MessageCountDto
            {
                Month = g.Key.Month,
                Year = g.Key.Year,
                TotalCount = g.Count(),
            })
            .OrderBy(a => a.Year)
            .ThenBy(a => a.Month);
    }
}