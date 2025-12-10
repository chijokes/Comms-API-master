using FusionComms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FusionComms.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IUserService userService;

        public RolesController(IUserService userService)
        {
            this.userService = userService;
        }


        [Authorize(Roles = "softwaredeveloper")]
        [HttpGet("CreateRole")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> CreateRoleAsync(string roleName)
        {
            try
            {
                if (await userService.RoleManager.RoleExistsAsync(roleName))
                {
                    throw new Exception("Role already exists.");
                }

                var result = await userService.RoleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                {
                    return Ok("Role created successfully");
                }

                throw new Exception(string.Join(",", result.Errors.Select(c => c.Description)));
            }
            catch
            {
                throw;
            }
        }

        [Authorize(Roles = "softwaredeveloper")]
        [HttpDelete("DeleteRole")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> DeleteRole(string roleName)
        {
            try
            {
                if (await userService.RoleManager.RoleExistsAsync(roleName) is false)
                {
                    throw new Exception("Role does not exist.");
                }

                var result = await userService.RoleManager.DeleteAsync(await userService.RoleManager.FindByNameAsync(roleName));

                if (result.Succeeded)
                {
                    return Ok("Role deleted successfully");
                }

                throw new Exception(string.Join(",", result.Errors.Select(c => c.Description)));
            }
            catch
            {
                throw;
            }
        }

        [HttpGet("ViewAllRoles")]
        [Authorize(Roles = "softwaredeveloper")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> ViewAllRoles()
        {
            try
            {
                var roles = await userService.RoleManager.Roles.Select(c => c.Name).ToListAsync();

                return Ok(roles);
            }
            catch
            {
                throw;
            }
        }
    }
}