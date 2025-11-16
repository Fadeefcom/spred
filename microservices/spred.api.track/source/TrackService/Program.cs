using System.Reflection;
using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;
using Repository.Abstractions.Extensions;
using Spred.Bus.Contracts;
using Spred.Bus.DependencyExtensions;
using TrackService.Abstractions;
using TrackService.Components.Consumers;
using TrackService.Components.Services;
using TrackService.Configuration;
using TrackService.DependencyExtensions;
using TrackService.Routes;

namespace TrackService;

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
        builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 100L * 1024 * 1024; });
        Console.WriteLine(
            $"{DateTime.Now} - Start configuration {Assembly.GetExecutingAssembly().GetName().Name}, Environment:{builder.Environment.EnvironmentName}");
        
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.AddConfigurationSections();
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
        builder.AddServiceDefaults(Assembly.GetExecutingAssembly().GetName().Name!);

        //Logging
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });

        //Configure options
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureBlobOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);

        //Authentication and Authorization
        builder.Services.AddAppAuthorization();
        builder.Services.AddJwtBearer(builder.Configuration, external: true);

        // Add services to the container.
        builder.Services.AddApplicationStores(builder.Configuration, builder.Environment.IsProduction());
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddSingleton<IAnalayzeTrackService, AnalayzeTrackService>();
        builder.Services.AddSingleton<IFFmpegWrapper, FFmpegWrapper>();
        builder.Services.AddScoped<TrackPlatformLinkService>();
        
        builder.Services.AddConsumer<TrackUpdateRequest, TrackUpdateConsumer>(16, 16);
        builder.Services.AddMassTransitFromRegistrations();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            await app.Services.CreateScope().InitTestTracks();
            app.UseCorsPolicy();
        }

        app.UseHttpLogging();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseAuthentication();
        app.AddHealthRoutes();
        app.AddMapGroup();
        app.AddMapGroupInternal();

        await DiExtensions.DownloadFfmpeg();

        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        Console.WriteLine($"{builder.Configuration.GetSection("ServicesOuterOptions")["UiEndpoint"]}");

        await app.RunAsync();
    }
}