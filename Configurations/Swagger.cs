using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FusionComms.Configurations
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
        {
            return services.AddSwaggerGen(c =>
            {
                // c.SwaggerDoc("v1", new OpenApiInfo
                // {
                //     Version = "v1",
                //     Title = "Fusion Comms API"
                //
                // });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = @"JWT Authorization header using the Bearer scheme. Example: 'Bearer eyJhbGci5'",
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }
    }
    
    public class SwaggerBasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        public SwaggerBasicAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }
    
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                if (context.Session.GetString("SwaggerAuthenticated") == "true" || context.Request.Path.StartsWithSegments("/swagger/login"))
                {
                    await _next.Invoke(context).ConfigureAwait(false);
                    return;
                }
                else
                {
                    context.Response.Redirect("/swagger/login");
                    return;
                }
            }
            else
            {
                await _next.Invoke(context).ConfigureAwait(false);
            }
        }
    }
}
