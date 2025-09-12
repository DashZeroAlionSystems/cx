using CX.Container.Server.Domain.FeedbackTypes;

namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Domain;
using HeimGuard;
using Mappings;
using MediatR;

public static class UpdateMessage
{
    public sealed record Command(Guid MessageId, MessageForUpdateDto UpdatedMessageData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHeimGuardClient _heimGuard;
        private readonly IAiService _aiService;
        private readonly ILogger<Handler> _logger;

        public Handler(IMessageRepository messageRepository, IUnitOfWork unitOfWork, IHeimGuardClient heimGuard, IAiService aiService, ILogger<Handler> logger)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _heimGuard = heimGuard;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _heimGuard.MustHavePermission<ForbiddenAccessException>(Permissions.CanManageMessages);

            var messageToUpdate = await _messageRepository.GetById(request.MessageId, cancellationToken: cancellationToken);
            var currentFeedback = FeedbackType.Of(messageToUpdate.Feedback); 
            
            var messageToAdd = request.UpdatedMessageData.ToMessageForUpdate();
            messageToUpdate.Update(messageToAdd);

            var newFeedback = FeedbackType.Of(messageToAdd.Feedback);
            
            _logger.LogInformation($"Checking user sentiment: Old feedback [{currentFeedback.Value}] - New feedback [{newFeedback.Value}]");
            _logger.LogInformation($"Thread linked to message: {messageToUpdate.ThreadId}");
            
            if (currentFeedback != newFeedback && newFeedback != FeedbackType.None() && messageToUpdate.ThreadId.HasValue)
            {
                var feedback = newFeedback == FeedbackType.Positive() ? 1 : -1;
                
                _logger.LogInformation($"Sending user sentiment: {feedback}");
                
                await _aiService.RateMessageAsync(messageToUpdate.ThreadId.Value, feedback);
            }

            _messageRepository.Update(messageToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}