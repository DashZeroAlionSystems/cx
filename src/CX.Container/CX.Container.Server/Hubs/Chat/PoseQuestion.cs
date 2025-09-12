using CX.Container.Server.Domain;
using CX.Container.Server.Domain.ContentTypes;
using CX.Container.Server.Domain.MessageCitations.Services;
using CX.Container.Server.Domain.Messages;
using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Mappings;
using CX.Container.Server.Domain.Messages.Models;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Domain.MessageTypes;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Extensions.Application;
using CX.Container.Server.Services;
using MediatR;

namespace CX.Container.Server.Hubs.Chat;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class PoseQuestion
{
    public sealed record Command(MessageForCreationDto Question) : IRequest<MessageDto>;

    public sealed class Handler : IRequestHandler<Command, MessageDto>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IThreadRepository _threadRepository;
        private readonly IMessageCitationRepository _citationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAiService _aiService;
        
        public Handler(
            IMessageRepository messageRepository,
            IThreadRepository threadRepository,
            IMessageCitationRepository citationRepository,
            IUnitOfWork unitOfWork,
            IAiService aiService)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _aiService = aiService;
            _threadRepository = threadRepository;
            _citationRepository = citationRepository;
        }
        
        public async Task<MessageDto> Handle(Command request, CancellationToken cancellationToken)
        {
            
            var question = request.Question.ToMessageForCreation();
            
            await EnsureThread(question);
            await SaveRequest(question);

            var aiResponse = await _aiService.SendMessage(question);

            return await CreateResponseFromRequest(question, aiResponse);
        }

        private async Task EnsureThread(MessageForCreation message)
        {
            if (message.ThreadId is not null) return;

            var thread = Domain.Threads.Thread.Create(message.Content.Truncate(50));
            await _threadRepository.Add(thread);
            await _unitOfWork.CommitChanges();
            
            message.ThreadId = thread.Id;
        }


        private async Task<Message> SaveRequest(MessageForCreation message)
        {
            var messageToSave = Message.Create(message);
            await _messageRepository.Add(messageToSave);
            await _unitOfWork.CommitChanges();
            
            return messageToSave;
        }
        
        private async Task<MessageDto> CreateResponseFromRequest(
            MessageForCreation request,
            AelaResponseDto content)
        {
            var model = new MessageForCreation()
            {
                Content = content.Message,
                ContentType = ContentType.PlainText(),
                MessageType = MessageType.System(),
                ThreadId = request.ThreadId,
                Citations = content.Citations
            };

            var message = Message.Create(model);
            await _messageRepository.Add(message);

            if (message.Citations != null) 
                await _citationRepository.AddRange(message.Citations);

            await _unitOfWork.CommitChanges();
            
            return message.ToMessageDto();
        }
    }
    
}