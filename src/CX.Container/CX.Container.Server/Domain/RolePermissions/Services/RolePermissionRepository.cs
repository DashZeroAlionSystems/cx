namespace CX.Container.Server.Domain.RolePermissions.Services;

using CX.Container.Server.Domain.RolePermissions;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IRolePermissionRepository : IGenericRepository<RolePermission, Guid>
{
}

public sealed class RolePermissionRepository : GenericRepository<RolePermission, Guid>, IRolePermissionRepository
{
    private readonly AelaDbContext _dbContext;

    public RolePermissionRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
