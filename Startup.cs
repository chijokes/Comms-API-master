using FusionComms.Configurations;
using FusionComms.DTOs.WhatsApp;
using FusionComms.Services.WhatsApp;
using FusionComms.Services.WhatsApp.Cinemas;
using FusionComms.Services.WhatsApp.Restaurants;
using Mailjet.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace FusionComms
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigurationDetails.Initialize(configuration);
        }

        public IConfiguration Configuration { get; }
        

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {


            services.AddAutoMapper(typeof(Startup));
            var connectionString = Configuration.GetConnectionString("connectionString");

            services.ConfigureDbContext(connectionString);

            services.ConfigureServices();

            //services.AddControllers()
            //    .AddNewtonsoftJson();

            services.ConfigureAuthentication(Configuration);
            services.ConfigureAuthorization();

            services.AddHttpClient("zepto", client =>
            {
                client.BaseAddress = new Uri("https://api.zeptomail.com");
                client.DefaultRequestHeaders.Add("Authorization",
                    Configuration.GetSection("ZeptoConfig:ApiKey").Value);
            });
            
            services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
            {
                client.DefaultRequestVersion = new Version("3.1");

                client.SetDefaultSettings();

                client.UseBasicAuthentication("cb1218e2f971b3a386f88aaaf002a95c", "95976b29e9eea11ee828b0d55bcaab16");
            });

            services.ConfigureSwagger();

            services.ConfigureAppSetting(Configuration);

            services.ConfigureDefaultIdentity();
            services.AddControllers()
                .AddNewtonsoftJson();
            services.AddHttpClient();
            services.AddScoped<WhatsAppMessagingService>();
            services.AddScoped<WhatsAppCatalogService>();
            services.AddScoped<WhatsAppOnboardingService>();
            services.AddScoped<OrderService>();
            services.AddScoped<OrderCartManager>();
            services.AddScoped<OrderValidationService>();
            services.AddScoped<OrderUIManager>();
            services.AddScoped<OrderSessionManager>();
            services.AddScoped<OrderStateManager>();
            services.AddScoped<OrderFlowEngine>();
            services.AddScoped<RestaurantWebhookProcessor>();
            services.AddScoped<CinemaWebhookProcessor>();
            services.AddScoped<WebhookProcessorFactory>();
            services.AddScoped<WhatsAppProfileService>();
            services.AddScoped<ProfileManager>();
            services.AddScoped<ProductSearchService>();
            services.AddScoped<IWhatsAppTemplateService, WhatsAppTemplateService>();
            services.AddRazorPages();
            
            services.AddDistributedMemoryCache(); 

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<LoggerMiddleware>();

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseSession();
                
            app.UseMiddleware<SwaggerBasicAuthMiddleware>();
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fusion Comms v1"));


            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors(option => option.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            //app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages(); 
            });
        }
    }
}
