using System.Reflection;
using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;
using Repository.Abstractions.Extensions;
using Spred.Bus.DependencyExtensions;
using ${ServiceName}.Routes;

namespace ${ServiceName};

/// <summary>
/// Program class for the service.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entrypoint for the application.
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        Console.WriteLine(
            $"{DateTime.Now} - Start configuration {Assembly.GetExecutingAssembly().GetName().Name}, Environment:{builder.Environment.EnvironmentName}");
        
        builder.AddConfigurationSections();
        builder.AddServiceDefaults(Assembly.GetExecutingAssembly().GetName().Name!);
        
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });
        
        //Configure options
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        
        var app = builder.Build();
        
        if (builder.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCorsPolicy();
        }
        
        app.UseHttpLogging();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseAuthentication();
        app.AddHealthRoutes();
        
        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        await app.RunAsync();
    }
    
}