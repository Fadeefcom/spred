using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Authorization.Configuration;
using Authorization.DataProtection;
using Authorization.DiExtensions;
using Authorization.Helpers;
using Authorization.Routes;
using Authorization.Services.Consumers;
using Extensions.DiExtensions;
using Extensions.Interfaces;
using Extensions.Models;
using Extensions.ServiceDefaults;
using Extensions.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Models;
using Spred.Bus.Contracts;
using Spred.Bus.DependencyExtensions;
using StackExchange.Redis;

namespace Authorization;


/// <summary>
/// Program class for the Authorization service.
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

        Console.WriteLine($"{DateTime.Now} - Start configuration {Assembly.GetExecutingAssembly().GetName().Name}, Environment:{builder.Environment.EnvironmentName}");
        builder.AddConfigurationSections();
        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.AddServiceDefaults(Assembly.GetExecutingAssembly().GetName().Name!);

        //Configure options
        builder.Services.AddGetToken();
        builder.Services.ConfigureJwtSettings(builder.Configuration);
        builder.Services.ConfigureDbConnectionOptions(builder.Configuration);
        builder.Services.ConfigureServicesOuterOptions(builder.Configuration);
        builder.Services.ConfigureRedisOptions(builder.Configuration);
        builder.Services.ConfigureRabbitOptions(builder.Configuration);
        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Default Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        });
        builder.Services.Configure<RoleValidationOptions>(o =>
        {
            o.MinNameLength = 1;
            o.MaxNameLength = 64;
            o.AllowedNameRegex = "^[A-Za-z0-9_.-]+$";
        });

        //Logging
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
        });
        builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();

        //Authentication and Authorization
        builder.Services.AddExtendedAppAuthorization()
            .AddAppAuthentication(builder.Configuration,
            builder.Environment.IsDevelopment());

        // Add services to the container.
        builder.Services.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
        builder.Services.AddServices();
        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddTransient<JwtSecurityTokenHandler>();
        builder.Services.AddSingleton<RedirectResponse>();
        builder.Services.AddAutoMapper(typeof(Program));
        builder.Services.AddSingleton<CookieHelper>();
        builder.Services.AddAvatarService(builder.Configuration);
        builder.Services.AddConsumer<VerifyAccountResult, VerifyAccountResultConsumer>(16, 16);
        builder.Services.AddMassTransitFromRegistrations();

        //Redis Data Protection
        builder.Services.AddSingleton<IXmlRepository, RedisXmlRepository>();
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>, RedisKeyManagementOptionsSetup>();
        builder.Services.AddDataProtection()
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
            .SetApplicationName("AuthorizationService");

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Services.GetRequiredService<IOptions<DbConnectionOptions>>().Value.EnsureCreated)
            await app.Services.CreateScope().InitRoles();

        if (builder.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            await app.Services.CreateScope().InitTestUser();
            app.UseCorsPolicy();
        }

        if (builder.Environment.EnvironmentName == "Production")
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
            });
        }
        
        app.Use(async (context, next) =>
        {
            if (!string.IsNullOrWhiteSpace(context.Request.Headers["X-Forwarded-Proto"].ToString()))
                context.Request.Scheme = context.Request.Headers["X-Forwarded-Proto"].ToString();
            await next();
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpLogging();
        app.AddExceptionHandler();
        app.AddRouteGroup();
        app.AddAccountRouteGroup();
        app.AddHealthRoutes();

        Console.WriteLine($"{DateTime.Now} - Run {Assembly.GetExecutingAssembly().GetName().Name}");

        await app.RunAsync();
    }
}