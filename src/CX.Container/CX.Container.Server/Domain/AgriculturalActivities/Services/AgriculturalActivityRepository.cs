namespace CX.Container.Server.Domain.AgriculturalActivities.Services;

using CX.Container.Server.Domain.AgriculturalActivities;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IAgriculturalActivityRepository : IGenericRepository<AgriculturalActivity, Guid>
{
}

public sealed class AgriculturalActivityRepository : GenericRepository<AgriculturalActivity, Guid>, IAgriculturalActivityRepository
{
    private readonly AelaDbContext _dbContext;

    public AgriculturalActivityRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
