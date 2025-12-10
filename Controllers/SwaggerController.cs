using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FusionComms.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SwaggerController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SwaggerController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("/swagger/login")]
        public IActionResult Login(string username, string password)
        {
            if (username == _configuration.GetSection("SwaggerLoginCredentials:Username").Value
                && password == _configuration.GetSection("SwaggerLoginCredentials:Password").Value)
            {
                HttpContext.Session.SetString("SwaggerAuthenticated", "true");
                return Redirect("/swagger");
            }
            else
            {
                return BadRequest("Invalid credentials");
            }
        }

        [HttpGet("/swagger/login")]
        public IActionResult LoginPage()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SwaggerLoginPage.html");
            
            if (System.IO.File.Exists(filePath)) return PhysicalFile(filePath, "text/html");

            return NotFound("Login page not found.");

        }
    }
}