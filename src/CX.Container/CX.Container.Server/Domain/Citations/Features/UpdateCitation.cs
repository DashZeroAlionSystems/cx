namespace CX.Container.Server.Domain.Citations.Features;

using CX.Container.Server.Domain.Citations.Dtos;
using CX.Container.Server.Domain.Citations.Services;
using CX.Container.Server.Domain.SourceDocuments.Services;
using CX.Container.Server.Services;
using MediatR;

public static class UpdateCitation
{
    public sealed record Command(Guid CitationId, CitationForUpdateDto UpdatedCitationsData) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly ICitationRepository _citationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(ICitationRepository citationRepository, IUnitOfWork unitOfWork)
        {
            _citationRepository = citationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {            
            var citationToUpdate = await _citationRepository.GetById(request.CitationId, cancellationToken: cancellationToken);
            citationToUpdate.Update(request.UpdatedCitationsData);

            _citationRepository.Update(citationToUpdate);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}