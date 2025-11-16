using System.Reflection;
using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;
using PlaylistService.Components.Consumers;
using PlaylistService.Configuration;
using PlaylistService.DependencyExtensions;
using PlaylistService.Routes;
using Repository.Abstractions.Extensions;
using Spred.Bus.Contracts;
using Spred.Bus.DependencyExtensions;

namespace PlaylistService;

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
        
        builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 16L * 1024 * 1024; });

        Console.WriteLine(
            $"{DateTime.Now} - Start configuration {Assembly.GetExecutingAssembly().GetName().Name}, Environment:{builder.Environment.EnvironmentName}");
        builder.AddConfigurationSections();
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
        builder.AddServiceDefaults(Assembly.GetExecutingAssembly().GetName().Name!);

        //Configure options
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);

        //Logging
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });

        //Authentication and Authorization
        builder.Services.AddAppAuthorization();
        builder.Services.AddJwtBearer(builder.Configuration, external: true);

        // AddAsync services to the container.
        builder.Services.AddGetToken();
        builder.Services.AddAppPlaylists();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddConsumer<CatalogEnrichmentUpdateOrCreate, PlaylistUpdateOrCreateConsumer>(1, 1);
        builder.Services.AddMassTransitFromRegistrations();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            await app.Services.CreateScope().InitTestPlaylistsAsync();
            app.UseCorsPolicy();
        }
        
        app.UseAuthentication();
        app.UseHttpLogging();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.AddHealthRoutes();
        app.AddMapGroup();
        app.AddMapGroupInternal();

        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        await app.RunAsync();
    }
}