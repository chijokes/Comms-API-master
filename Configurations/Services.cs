using FusionComms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FusionComms.Configurations
{
    public static class Services
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services) => services

            .AddScoped<IRepository, Repository>()
            .AddScoped<IOTPService, OTPService>()
            .AddScoped<ISmsService, SmsService>()
            .AddScoped<IAmazonSESService, AmazonSESService>()
            .AddScoped<IEmailService, EmailService>()
            .AddScoped<IMontyService, MontyService>()
            .AddScoped<IMailJetService, MailJetService>()
            .AddScoped<IZeptoMailService, ZeptoMailService>()
            .AddScoped<IUserService, UserService>();
    }
}
