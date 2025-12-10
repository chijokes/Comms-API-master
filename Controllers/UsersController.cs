using AutoMapper;
using FusionComms.DTOs;
using FusionComms.Entities;
using FusionComms.Services;
using FusionComms.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Controllers
{

    [Authorize]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly IAmazonSESService sesService;
        private readonly IMontyService montyService;
        private readonly IMapper mapper;

        public UsersController(IUserService userService, IMapper mapper, IMontyService montyService, IAmazonSESService sesService)
        {
            this.userService = userService;
            this.mapper = mapper;
            this.montyService = montyService;
            this.sesService = sesService;
        }


        [HttpPost("Create")]
        [Authorize(Roles = "softwaredeveloper")]
        public async Task<IActionResult> Create(CreateUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            var user = mapper.Map<User>(model);

            user.UserName = model.Email;

            var result = await userService.AddUser(user, model.Password);

            if(result is true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, "Created"));
            }

            return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
        }



        [HttpDelete("Delete")]
        [Authorize(Roles = "softwaredeveloper")]
        public async Task<IActionResult> Delete(string userId)
        {

            var user = await userService.CheckUser(userId);

            if (user is null)
            {
                return BadRequest(Util.BuildResponse(404, "Not Found", null, "User not found"));
            }

            var deleteResult = await userService.DeleteUser(user);

            if(deleteResult is true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, "Deleted"));
            }

            return BadRequest(Util.BuildResponse(400, "Bad Request", null, "Failed to delete"));
        }


        [HttpGet("GetUsers")]
        [Authorize(Roles = "softwaredeveloper")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await userService.GetUsers();

            if(users.Count == 0)
            {
                return NotFound(Util.BuildResponse(404, "Not Found", null, "User not found"));
            }

            return Ok(Util.BuildResponse(200, "Ok", null, users));
        }


        [HttpPut("RegisterOnMonty")]
        public async Task<IActionResult> RegisterOnMonty(string userId, AddMontyUser montyUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            if(await userService.CheckUser(userId) == null)
            {
                return NotFound(Util.BuildResponse(404, "Not Found", null, "User not found"));
            }

            var montyUserToAdd = mapper.Map<RegisteredMontyUser>(montyUser);
            montyUserToAdd.UserId = userId;

            var addResult = await montyService.Add(montyUserToAdd);

            if(addResult is true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, "Created"));
            }

            return BadRequest(Util.BuildResponse(400, "Bad Request", null, "Failed to add"));
        }


        [HttpPut("RegisterOnSes")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterOnSes(string userId, AddSesUser model, CancellationToken token)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Util.BuildResponse<object>(400, "Bad Request", ModelState, null));
            }

            if (await userService.CheckUser(userId) == null)
            {
                return NotFound(Util.BuildResponse(404, "Not Found", null, "User not found"));
            }

            var montyUserToAdd = mapper.Map<RegisteredSesUser>(model);
            montyUserToAdd.UserId = userId;

            var addResult = await sesService.Create(montyUserToAdd, token);

            if (addResult is true)
            {
                return Ok(Util.BuildResponse(200, "Ok", null, "Created"));
            }

            return BadRequest(Util.BuildResponse(400, "Bad Request", null, "Failed to add"));
        }

        [HttpPost("Authorize")]
        [AllowAnonymous]
        //[Authorize(Roles = "softwaredeveloper")]
        public async ValueTask<IActionResult> Authrorize([FromBody] LoginModel login)
        {
            try
            {
                return Ok(await userService.AuthorizeAsync(login));
            }
            catch
            {
                throw;
            }
        }
    }
}
