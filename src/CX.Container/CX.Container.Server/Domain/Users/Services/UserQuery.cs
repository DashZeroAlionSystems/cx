using CX.Container.Server.Databases;
using CX.Container.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CX.Container.Server.Domain.Users.Services;

public interface IUserQuery : IGenericQueryRepository<User>
{
    IAsyncEnumerable<string> GetIncompleteUsersIds();
}

public class UserQuery : GenericQueryRepository<User>, IUserQuery
{
    public UserQuery(AelaDbReadContext dbReadContext) : base(dbReadContext)
    {
    }
    public IAsyncEnumerable<string> GetIncompleteUsersIds()
    {
        return Query()
            .Where(u => string.IsNullOrEmpty(u.FirstName)
                        && string.IsNullOrEmpty(u.LastName)
                        && string.IsNullOrEmpty(u.Email)
            )
            .Select(u => u.Id)
            .AsAsyncEnumerable();
    }
}