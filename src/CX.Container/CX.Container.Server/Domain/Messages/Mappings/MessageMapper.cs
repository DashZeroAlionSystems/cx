using CX.Container.Server.Domain.MessageTypes;

namespace CX.Container.Server.Domain.Messages.Mappings;

using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Models;
using Riok.Mapperly.Abstractions;

[Mapper]
[UseStaticMapper(typeof(FromUserMapper))]
public static partial class MessageMapper
{
    public static partial MessageForCreation ToMessageForCreation(this MessageForCreationDto messageForCreationDto);
    public static partial MessageForUpdate ToMessageForUpdate(this MessageForUpdateDto messageForUpdateDto);
    
    [MapDerivedType<Message, MessageDto>]
    public static partial MessageDto ToMessageDto(this Entity<Guid> message);
    public static partial IQueryable<MessageDto> ToMessageDtoQueryable(this IQueryable<Message> message);
    public static partial IQueryable<MessageForChatDto> ToMessageForChatDtoQueryable(this IQueryable<Message> message);
    
    [MapProperty(nameof(Message.Content), nameof(MessageForChatDto.Message))]
    [MapProperty(nameof(Message.MessageType), nameof(MessageForChatDto.FromUser))]
    public static partial MessageForChatDto ToMessageForChatDto(this MessageForCreation message);
    
    [MapProperty(nameof(Message.Content), nameof(MessageForChatDto.Message))]
    [MapProperty(nameof(Message.MessageType), nameof(MessageForChatDto.FromUser))]
    public static partial MessageForChatDto ToMessageForChatDto(this Message message);

    [MapperIgnoreTarget(target:nameof(ConversationDto.History))]
    [MapProperty(nameof(Message.Content), nameof(ConversationDto.Message))]
    [MapProperty(nameof(Message.ThreadId), nameof(ConversationDto.UserId))]
    public static partial ConversationDto ToConversationDto(this MessageForCreation dto);
}

public static class FromUserMapper
{
    public static bool ToFromUser(this MessageType messageType) => messageType == MessageType.User();
    public static bool ToFromUser(this string messageType) => MessageType.Of(messageType) == MessageType.User();
}