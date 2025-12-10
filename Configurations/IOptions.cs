using FusionComms.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FusionComms.Configurations
{
    public static class IOptionsConfiguration
    {
        public static IServiceCollection ConfigureAppSetting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JWT>(configuration.GetSection("JWT"));

            return services;
        }
    }
}
