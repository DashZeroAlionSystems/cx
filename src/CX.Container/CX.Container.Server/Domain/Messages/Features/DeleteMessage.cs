namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Messages.Services;
using CX.Container.Server.Domain.Threads.DomainEvents;
using CX.Container.Server.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class DeleteMessage
{
    public sealed record Command(Guid MessageId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<Handler> _logger;

        public Handler(IMessageRepository messageRepository, IUnitOfWork unitOfWork, ILogger<Handler> logger)
        {
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await _unitOfWork.WrapInTransactionAsync(async (db, ct) =>
            {
                await DeleteMessage(db, ct);
            }, cancellationToken);

            // local method
            async Task DeleteMessage(AelaDbContext db, CancellationToken ct)
            {
                try
                {
                    db.QueueDomainEvent(new MessageDeleted { Id = request.MessageId });

                    await db.MessageCitations.Where(mc => mc.MessageId == request.MessageId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));
                    await db.Messages.Where(m => m.Id == request.MessageId).ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDeleted, true));

                    _logger.LogInformation("Deleted message {ThreadId}", request.MessageId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Delete of message {ThreadId} failed", request.MessageId);
                    throw;
                }
            }
        }
    }
}