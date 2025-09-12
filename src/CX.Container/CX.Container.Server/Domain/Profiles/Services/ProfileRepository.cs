namespace CX.Container.Server.Domain.Profiles.Services;

using CX.Container.Server.Domain.Profiles;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IProfileRepository : IGenericRepository<Profile, Guid>
{
}

public sealed class ProfileRepository : GenericRepository<Profile, Guid>, IProfileRepository
{
    private readonly AelaDbContext _dbContext;

    public ProfileRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
