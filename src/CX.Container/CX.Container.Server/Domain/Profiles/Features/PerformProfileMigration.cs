namespace CX.Container.Server.Domain.Profiles.Features;


using CX.Container.Server.Services;

public class PerformProfileMigration
{
    private readonly IUnitOfWork _unitOfWork;

    public PerformProfileMigration(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public sealed class Command
    {
        public string User { get; set; }
    }
    
    // [Queue(Consts.HangfireQueues.PerformProfileMigration)]
    
    public async Task Handle(Command command, CancellationToken cancellationToken)
    {
        // TODO some work here
        await _unitOfWork.CommitChanges(cancellationToken);
    }
}