using System.Reflection;
using AggregatorService.Abstractions;
using AggregatorService.BackgroundTasks;
using AggregatorService.Components;
using AggregatorService.Components.Consumers;
using AggregatorService.Configurations;
using AggregatorService.DependencyExtensions;
using AggregatorService.Routes;
using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.Models;
using Extensions.ServiceDefaults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Spred.Bus.Contracts;
using Spred.Bus.DependencyExtensions;
using StackExchange.Redis;

namespace AggregatorService;

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
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        //Configure options
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);
        builder.Services.AddOptions<ChartmetricOptions>()
            .Bind(builder.Configuration.GetSection(ChartmetricOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.Configure<SpotifyCredentialsList>(
            builder.Configuration.GetSection("SpotifyCredentialsList"));
        builder.Services
            .AddOptions<SoundchartsOptions>()
            .Bind(builder.Configuration.GetSection(SoundchartsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        //Logging
        builder.Services.AddHttpLogging(logging => { logging.LoggingFields = HttpLoggingFields.All; });

        //Authentication and Authorization
        builder.Services.AddAppAuthorization();
        builder.Services.AddJwtBearer(builder.Configuration, external: true);

        // Add services to the container.
        builder.Services.AddGetToken();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddSingleton<ITrackDownloadService, TrackDownloadService>();
        builder.Services.AddSingleton<ITrackSenderService, TrackSenderService>();
        builder.Services.AddSingleton<IParserAccessGate, ParserAccessGate>();
        builder.Services.AddSingleton<ICatalogService, CatalogService>();
        builder.Services.AddSingleton<ISpotifyTokenProvider, SpotifyTokenProvider>();
        builder.Services.AddSingleton<ICatalogProvider, SoundchartsCatalogProvider>();
        builder.Services.AddCosmosClient();
        builder.Services.AddRestServices();
        builder.Services.AddHostedService<DailyPlaylistCronTask>();
        builder.Services.AddSingleton<IChartmetricsTokenProvider, ChartmetricsTokenProvider>();
        builder.Services.AddSingleton<IApiRateLimiter, ChartmetricsRateLimiter>();
        builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
        
        builder.Services.AddConsumer<CatalogEnrichmentRequest, CatalogEnrichmentRequestConsumer>(16, 16);
        builder.Services.AddConsumer<AggregateCatalogReport, AggregateCatalogReportConsumer>(1, 1);
        builder.Services.AddConsumer<VerifyAccountCommand, VerifyAccountConsumer>(16, 16);
        builder.Services.AddMassTransitFromRegistrations();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCorsPolicy();
        app.UseAuthentication();
        app.UseHttpLogging();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.AddHealthRoutes();
        app.AddMapGroup();
        app.AddMapGroupInternal();
        
        await app.RunAsync();
    }
}