using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Mappings;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Resources;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace CX.Container.Server.Services;

public interface IConversationCache
{
    Task<List<MessageForChatDto>> GetConversation(Guid threadId, CancellationToken cancellationToken = default);
    ValueTask SetConversation(Guid threadId, List<MessageForChatDto> conversation);
}

public class ConversationCache : IConversationCache
{
    private readonly IFusionCache _cache;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ConversationCache> _logger;
    
    public ConversationCache(
        IFusionCacheProvider cacheProvider,
        IMessageRepository messageRepository,
        ILogger<ConversationCache> logger)
    {
        _cache = cacheProvider.GetCache(Consts.Cache.Conversation.Name);
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<List<MessageForChatDto>> GetConversation(Guid threadId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrSetAsync
        (
            Consts.Cache.Conversation.ThreadKey(threadId),
            async ct =>
            {
                _logger.LogDebug("Cache miss for conversation thread {ThreadId}", threadId);

                return await _messageRepository.GetByThreadId(threadId).ToMessageForChatDtoQueryable().ToListAsync(ct);
            },
            token: cancellationToken
        );
    }

    public async ValueTask SetConversation(Guid threadId, List<MessageForChatDto> conversation)
    {
        await _cache.SetAsync
        (
            Consts.Cache.Conversation.ThreadKey(threadId),
            conversation
        );
    }
}