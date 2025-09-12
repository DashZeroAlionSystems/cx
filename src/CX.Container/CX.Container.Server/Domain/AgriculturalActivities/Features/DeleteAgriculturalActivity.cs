namespace CX.Container.Server.Domain.AgriculturalActivities.Features;

using CX.Container.Server.Domain.AgriculturalActivities.Services;
using CX.Container.Server.Services;
using MediatR;

public static class DeleteAgriculturalActivity
{
    public sealed record Command(Guid AgriculturalActivityId) : IRequest;

    public sealed class Handler : IRequestHandler<Command>
    {
        private readonly IAgriculturalActivityRepository _agriculturalActivityRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IAgriculturalActivityRepository agriculturalActivityRepository, IUnitOfWork unitOfWork)
        {
            _agriculturalActivityRepository = agriculturalActivityRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var recordToDelete = await _agriculturalActivityRepository.GetById(request.AgriculturalActivityId, cancellationToken: cancellationToken);
            _agriculturalActivityRepository.Remove(recordToDelete);
            await _unitOfWork.CommitChanges(cancellationToken);
        }
    }
}