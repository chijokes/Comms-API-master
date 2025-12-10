using FusionComms.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FusionComms.Configurations
{
    public static class AuthenticationConfiguration
    {
        public static AuthenticationBuilder ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            JWT jwt = configuration.GetSection("JWT").Get<JWT>();

            return services.AddAuthentication()
              .AddCookie(options =>
              {
                  options.SlidingExpiration = true;
              })
              .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
              {
                  options.RequireHttpsMetadata = false;
                  options.SaveToken = true;
                  options.TokenValidationParameters = new TokenValidationParameters
                  {
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwt.SigningKey)),
                      ValidateAudience = false,
                      ValidateIssuer = false
                  };
              });
        }
    }
}
