namespace CX.Container.Server.Domain.Messages.Features;

using CX.Container.Server.Domain.Citations.Dtos;
using CX.Container.Server.Domain.Citations.Services;
using MediatR;

public static class GetCitation
{
    public sealed record Query(Guid CitationId) : IRequest<CitationDto>;

    public sealed class Handler : IRequestHandler<Query, CitationDto>
    {
        private readonly ICitationRepository _citationRepository;

        public Handler(ICitationRepository citationRepository)
        {
            _citationRepository = citationRepository;
        }

        public async Task<CitationDto> Handle(Query request, CancellationToken cancellationToken)
        {
            var result = await _citationRepository.GetById(request.CitationId, cancellationToken: cancellationToken);
            return new CitationDto()
            {
                Content = result.Content,
                Description = result.Description,
                Name = result.Name,
                FileType = result.FileType
            };
        }
    }
}