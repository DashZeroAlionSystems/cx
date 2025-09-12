namespace CX.Container.Server.Domain.Projects.Services;

using CX.Container.Server.Domain.Projects;
using CX.Container.Server.Databases;
using CX.Container.Server.Services;

public interface IProjectRepository : IGenericRepository<Project, Guid>
{
}

public sealed class ProjectRepository : GenericRepository<Project, Guid>, IProjectRepository
{
    private readonly AelaDbContext _dbContext;

    public ProjectRepository(AelaDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}
