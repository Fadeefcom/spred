using Extensions.DiExtensions;
using Extensions.Middleware;
using Extensions.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;
using Repository.Abstractions.Extensions;
using Spred.Bus.DependencyExtensions;
using SubscriptionService.Configurations;
using SubscriptionService.Routes;
using System.Reflection;
using Spred.Bus.Abstractions;
using Spred.Bus.Services;
using SubscriptionService.DependencyExtensions;

namespace SubscriptionService;

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
        
        builder.Services.AddAppAuthorization();
        builder.Services.AddAuthentication();
        builder.Services.AddJwtBearer(builder.Configuration, external: true);
        builder.Services.AddJwtBearer(builder.Configuration, external: false);
        
        //Configure options
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);
        builder.Services
            .AddOptions<StripeOptions>()
            .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddScoped<IActorProvider, HttpContextActorProvider>();
        builder.Services.AddScoped<IActivityWriter, RabbitActivityWriter>();
        builder.Services.AddSubscriptionServices();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddMassTransitFromRegistrations();
        
        var app = builder.Build();
        
        if (builder.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCorsPolicy();
            app.UseDeveloperExceptionPage();
        }
        
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseHttpLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.AddHealthRoutes();
        app.MapSubscriptionRoutes();
        app.MapInternalSubscriptionRoutes();
        
        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        await app.RunAsync();
    }
    
}