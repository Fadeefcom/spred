using System.Reflection;
using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.ServiceDefaults;
using InferenceService.Abstractions;
using InferenceService.Components;
using InferenceService.Components.Consumers;
using InferenceService.Configuration;
using InferenceService.DependencyExtensions;
using InferenceService.Helpers;
using InferenceService.Models.Dto;
using InferenceService.Routes;
using Microsoft.AspNetCore.HttpLogging;
using Repository.Abstractions.Extensions;
using Spred.Bus.DependencyExtensions;

namespace InferenceService;

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
        builder.Services.AddOptions<ModelVersion>().Bind(builder.Configuration.GetSection(ModelVersion.SectionName))
            .ValidateOnStart();
        builder.Services.AddOptions<BlobOptions>().Bind(builder.Configuration.GetSection(BlobOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName) ||
                                 !string.IsNullOrWhiteSpace(options.BlobConnectString), "Blob options is empty.")
            .ValidateOnStart();

        //Logging
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });

        //Authentication and Authorization
        builder.Services.AddAppAuthorization();
        builder.Services.AddJwtBearer(builder.Configuration, external: true);

        // Add services to the container.
        builder.Services.AddAppServices();
        builder.Services.AddApplicationStores(builder.Environment.IsProduction());
        builder.Services.AddGetToken();
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddScoped<ITrackServiceHelper, TrackServiceHelper>();
        builder.Services.AddSingleton<IFFmpegWrapper, FFmpegWrapper>();
        builder.Services.AddSingleton<WaveFormatHelper>();
        builder.Services.AddSingleton<IInferenceAccessService, InferenceAccessService>();
        
        builder.Services.AddConsumer<TrackEmbeddingResult, InferenceEmbeddingConsumer>(16, 16);
        builder.Services.AddMassTransitFromRegistrations();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCorsPolicy();
        }

        app.UseAuthentication();
        app.UseHttpLogging();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.AddMapGroup();
        app.AddMapInternalGroup();
        app.AddHealthRoutes();

        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        await app.RunAsync();
    }
}