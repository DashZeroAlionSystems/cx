using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CX.Container.Server.Common
{

    
    public class RequiresRoleAttribute : ActionFilterAttribute
    {
        public List<string> GetRequiredRoles{ get; } = new();

        public RequiresRoleAttribute(params string[] roles)
        {
            GetRequiredRoles.AddRange(roles);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity == null || context.HttpContext.User.Identity.IsAuthenticated == false)
            {
                context.Result = new StatusCodeResult(403);
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            foreach (var role in GetRolesFromToken(context.HttpContext.User))
            {
                if (GetRequiredRoles.Any(x => string.Equals(x, role, StringComparison.InvariantCultureIgnoreCase))) continue;

                //Sending a 401 directs you back to the login page (which we want to avoid)
                //Use Forbidden to show no access: https://leastprivilege.com/2014/10/02/401-vs-403/
                context.Result = new StatusCodeResult(403);
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            base.OnActionExecuting(context);
        }
        public static List<string> GetRolesFromToken(ClaimsPrincipal token)    => token.Claims.Where(claim => claim.Type == "role" || claim.Type == ClaimTypes.Role).Select(permission => permission.Value).ToList();
    }
}