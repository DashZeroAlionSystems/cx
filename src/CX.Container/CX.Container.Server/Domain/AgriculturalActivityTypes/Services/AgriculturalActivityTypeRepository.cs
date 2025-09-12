namespace CX.Container.Server.Domain.AgriculturalActivityTypes.Services;

using CX.Container.Server.Domain.AgriculturalActivityTypes;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IAgriculturalActivityTypeRepository : IGenericRepository<AgriculturalActivityType, Guid>
{
}

public sealed class AgriculturalActivityTypeRepository : GenericRepository<AgriculturalActivityType, Guid>, IAgriculturalActivityTypeRepository
{
    private readonly AelaDbContext _dbContext;

    public AgriculturalActivityTypeRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
