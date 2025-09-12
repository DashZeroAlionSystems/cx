using Aela.Server.Common;

namespace CX.Container.Server.Common;

public class RequiresAtLeastUserRoleAttribute : RequiresRoleAttribute
{
    public RequiresAtLeastUserRoleAttribute() : base(SecurityConstants.Roles.AtLeastUser)
    {
    }
}