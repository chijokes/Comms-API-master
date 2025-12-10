using System;
using FusionComms;
using FusionComms.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessName()
            .Enrich.WithProperty("ProjectName", "fusion-comms")
            .WriteTo.Console()
            .WriteTo.Seq("https://seq-log.reachcinema.io")
            .CreateLogger();
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            //NLog: catch setup errors
            if (ex.InnerException?.Message != null) Log.Error(ex, ex.InnerException?.Message);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

//var builder = WebApplication.CreateBuilder(args);

//var conn = builder.Configuration.GetConnectionString("connectionString");
//// Add services to the container.

//builder.Services.AddAutoMapper(typeof(Program));
//builder.Services.ConfigureDbContext(conn);
//builder.Services.ConfigureAppSetting(builder.Configuration);
//builder.Services.ConfigureAuthentication(builder.Configuration);
//builder.Services.ConfigureServices();
//builder.Services.ConfigureAuthorization();
//builder.Services.ConfigureSwagger();
//builder.Services.ConfigureDefaultIdentity();
//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
////builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseMiddleware<ExceptionMiddleware>();

//app.UseHttpsRedirection();

//app.UseAuthentication();

//app.UseAuthorization();

//app.UseCors(option => option.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

//app.MapControllers();

//app.Run();
