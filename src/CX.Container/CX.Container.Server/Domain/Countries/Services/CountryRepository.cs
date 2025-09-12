namespace CX.Container.Server.Domain.Countries.Services;

using CX.Container.Server.Domain.Countries;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface ICountryRepository : IGenericRepository<Country, Guid>
{
}

public sealed class CountryRepository : GenericRepository<Country, Guid>, ICountryRepository
{
    private readonly AelaDbContext _dbContext;

    public CountryRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
