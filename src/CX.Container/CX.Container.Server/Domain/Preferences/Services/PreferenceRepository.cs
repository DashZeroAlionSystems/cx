namespace CX.Container.Server.Domain.Preferences.Services;

using CX.Container.Server.Domain.Preferences;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IPreferenceRepository : IGenericRepository<Preference, Guid>
{
}

public sealed class PreferenceRepository : GenericRepository<Preference, Guid>, IPreferenceRepository
{
    private readonly AelaDbContext _dbContext;

    public PreferenceRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
