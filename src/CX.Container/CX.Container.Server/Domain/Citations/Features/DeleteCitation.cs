namespace CX.Container.Server.Domain.Citations.Features;

using CX.Container.Server.Domain.Citations.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteCitation
{
    public sealed record Command(Guid CitationId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ICitationRepository _citationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<Handler> _logger;

        public Handler(ICitationRepository citationRepository, IUnitOfWork unitOfWork, ILogger<Handler> logger)
        {
            _citationRepository = citationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {               
            try
            {
                var recordToDelete = await _citationRepository.GetById(request.CitationId, cancellationToken: cancellationToken);
                _citationRepository.Remove(recordToDelete);
                await _unitOfWork.CommitChanges(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Delete of citation {ThreadId} failed", request.CitationId);
                throw;
            }            
        }
    }
}