using FusionComms.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FusionComms.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAuthorizer : Attribute, IAllowAnonymous, IAuthorizationFilter
    {
        private readonly string Claim;
        private readonly AuthorizationCheckType CheckType;
        public CustomAuthorizer(string claim, AuthorizationCheckType authorizationCheckType)
        {
            Claim = claim;
            CheckType = authorizationCheckType;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (CheckType == AuthorizationCheckType.AuthorizeByRole)
            {
                var result = AuthorizeByRoleClaims(context);
                if (result != null)
                    return;
            }
            else if (CheckType == AuthorizationCheckType.AuthorizeByUser)
            {
                var contextResult = AuthorizeByUserClaims(context);
                if (contextResult != null)
                    return;
            }

            //context.Result = new UnauthorizedResult();
            return;
        }

        private AuthorizationFilterContext AuthorizeByRoleClaims(AuthorizationFilterContext context)
        {
            var dbContext = context.HttpContext
                  .RequestServices
                  .GetService(typeof(AppDbContext)) as AppDbContext;

            var UserRoleName = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            

            var role = dbContext.Roles.FirstOrDefault(c => c.Name == UserRoleName);
            if (role == null)
            {
                context.Result = new UnauthorizedResult();
                return context;
            }

            var roleContainsClaim = dbContext.RoleClaims.FirstOrDefault(c => c.RoleId == role.Id && c.ClaimValue == Claim);
            if (roleContainsClaim == null)
            {
                context.Result = new UnauthorizedResult();
                return context;
            }

            return null;
        }

        private AuthorizationFilterContext AuthorizeByUserClaims(AuthorizationFilterContext context)
        {
            var dbContext = context.HttpContext
                  .RequestServices
                  .GetService(typeof(AppDbContext)) as AppDbContext;

            var UserRoleName = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);


            if (UserRoleName == null)
            {
                context.Result = new UnauthorizedResult();
                return context;
            }

            var userHasClaim = dbContext.UserClaims.FirstOrDefault(c => c.UserId == userId && c.ClaimValue == Claim);
            if (userHasClaim == null)
            {
                context.Result = new ForbidResult();
                return context;
            }

            return null;
        }
    }

    public enum AuthorizationCheckType
    {
        /// <summary>
        /// Checks if the role has the requested claim to it
        /// </summary>
        AuthorizeByRole,

        /// <summary>
        /// Checks if the role has the requested claim to it
        /// </summary>
        AuthorizeByUser
    }
}
