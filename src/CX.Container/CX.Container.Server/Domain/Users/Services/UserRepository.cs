using CX.Container.Server.Databases;
using CX.Container.Server.Domain.Users.Dtos;
using CX.Container.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Users.Services;

public interface IUserRepository : IGenericRepository<User, string>
{
    public bool HasUsers();
    public bool HasUser(string id);
    public IEnumerable<string> GetRolesByUserIdentifier(string id);
    public Task AddRole(UserRole entity, CancellationToken cancellationToken = default);
    public void RemoveRole(UserRole entity);
    public Task<List<CreatedUserSummaryDto>> GetUserCountOverTimeAsync(CancellationToken cancellationToken = default);
}

public class UserRepository : GenericRepository<User, string>, IUserRepository
{
    private readonly AelaDbContext _dbContext;

    public UserRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<User> GetByIdOrDefault(string id, bool withTracking = true,
        CancellationToken cancellationToken = default)
    {
        return withTracking
            ? await _dbContext.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(e => Equals(e.Id, id), cancellationToken)
            : await _dbContext.Users
                .Include(u => u.Roles)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => Equals(e.Id, id), cancellationToken);
    }

    public async Task AddRole(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public void RemoveRole(UserRole userRole)
    {
        _dbContext.UserRoles.Remove(userRole);
    }

    public bool HasUsers()
    {
        return _dbContext.Users.Any(x => x.IsDeleted == false);
    }

    public bool HasUser(string id)
    {
        return _dbContext.Users.Any(x => x.Id == id);
    }

    public IEnumerable<string> GetRolesByUserIdentifier(string id)
    {
        return _dbContext.UserRoles
            .Include(x => x.User)
            .Where(x => x.User.Id == id)
            .Select(x => x.Role.Value);
    }

    public async Task<List<CreatedUserSummaryDto>> GetUserCountOverTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .GroupBy(a => new { a.CreatedOn.Month, a.CreatedOn.Year })
            .Select(g => new CreatedUserSummaryDto
            {
                Month = g.Key.Month,
                Year = g.Key.Year, 
                TotalCount = g.Count(),
                ActiveCount = g.Count(x => x.IsDeleted == false)
            })
            .OrderBy(a => a.Year)
            .ThenBy(a => a.Month)
            .ToListAsync();
    }
}