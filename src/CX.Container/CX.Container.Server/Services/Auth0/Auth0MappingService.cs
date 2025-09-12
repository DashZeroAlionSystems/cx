using CX.Container.Server.Domain.Users.Models;
using CX.Container.Server.Extensions.Application;
using Auth0.ManagementApi.Models;

namespace CX.Container.Server.Services.Auth0;

public interface IAuth0MappingService
{
    UserForUpdate MapUserToUserForUpdate(User iamUser);
}

public class Auth0MappingService : IAuth0MappingService
{
    public UserForUpdate MapUserToUserForUpdate(User iamUser)
    {
        var user = new UserForUpdate();
        
        var providerFirstName = iamUser.ProviderAttributes?.GetValueOrDefault("first_name")?.ToString();
        var providerLastName = iamUser.ProviderAttributes?.GetValueOrDefault("last_name")?.ToString();
        
        user.FirstName = iamUser.FirstName.Coalesce(providerFirstName, iamUser.NickName);
        user.LastName = iamUser.LastName.Coalesce(providerLastName);
        user.Email = iamUser.Email;
        user.Username = iamUser.UserName.Coalesce(iamUser.FullName, iamUser.NickName);
        // Locale
        // Picture
        // PhoneNumber

        return user;
    }
}