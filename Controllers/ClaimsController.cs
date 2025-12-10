using FusionComms.DTOs;
using FusionComms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FusionComms.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private readonly string[] defaultClaims =
        {
            "Send Single SMS", "Send OTP", "Verify OTP"
        };

        private readonly IUserService userService;

        public ClaimsController(IUserService userService)
        {
            this.userService = userService;
        }

        [Authorize(Roles = "softwaredeveloper")]
        [HttpGet("ViewAllClaims")]
        public IActionResult ViewAllClaims()
        {
            return Ok(defaultClaims);
        }

        [Authorize(Roles = "softwaredeveloper")]
        [HttpPost("AddClaimsToRoles")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> AddClaimsToRoles(ClaimRequest[] requests)
        {
            try
            {
                var result = await userService.AddClaimsToRole(requests);

                if (result is true)
                    return Ok("Claims added successfully");
                return BadRequest("Claims not added successfully.");
            }
            catch
            {
                throw;
            }
        }



        [Authorize(Roles = "softwaredeveloper")]
        [HttpPost("AddClaimsToUser")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddClaimsToUser(ClaimRequestForUser[] request)
        {
            var addResult = await userService.AddClaimsToUser(request);


            if (addResult is true)
                return Ok("Claims added successfully");
            return BadRequest("Claims not added successfully.");
        }



        [Authorize(Roles = "softwaredeveloper")]
        [HttpPost("RemoveClaimsFromUser")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveClaimsFromUser(ClaimRequestForUser[] request)
        {
            var addResult = await userService.RemoveClaimsFromUser(request);


            if (addResult is true)
                return Ok("Claims removed successfully");
            return BadRequest("Claims not removed successfully.");
        }


        [Authorize(Roles = "softwaredeveloper")]
        [HttpPost("RemoveClaimsFromRoles")]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> RemoveClaimsFromRoles(ClaimRequest[] requests)
        {
            try
            {
                foreach (var request in requests)
                {
                    var role = await userService.RoleManager.FindByNameAsync(request.RoleName);

                    if (role is null)
                    {
                        throw new Exception($"Role [{role}] does not exist.");
                    }

                    var claims = await userService.RoleManager.GetClaimsAsync(role);

                    foreach (var claim in request.Claims)
                    {
                        if (claims.Any(c => c.Value == claim))
                        {
                            await userService.RoleManager.RemoveClaimAsync(role, new Claim(ClaimTypes.Actor, claim));
                        }
                    }
                }

                return Ok("Claims removed successfully.");
            }
            catch
            {
                throw;
            }
        }



        
        [Authorize(Roles = "softwaredeveloper")]
        [HttpGet("ViewClaimsByUser")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> ViewClaimsByUser(string userName)
        {
            try
            {
                var user = await userService.UserManager.FindByNameAsync(userName);

                if (user is null)
                {
                    throw new Exception($"User [{user}] does not exist.");
                }

                var claims = await userService.GetClaimsByUser(user);

                return Ok(claims);
            }
            catch
            {
                throw;
            }
        }


        [Authorize(Roles = "softwaredeveloper")]
        [HttpGet("ViewClaimsByRole")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        public async ValueTask<IActionResult> ViewClaimsByRole(string roleName)
        {
            try
            {
                var role = await userService.RoleManager.FindByNameAsync(roleName);

                if (role is null)
                {
                    throw new Exception($"RoleName [{role}] does not exist.");
                }

                var claims = await userService.GetClaimsByRole(role);

                return Ok(claims);
            }
            catch
            {
                throw;
            }
        }
    }
}
