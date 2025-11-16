using System.Diagnostics;
using Authorization.Abstractions;
using Authorization.DAL;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Services;
using Authorization.Validators;
using CloudinaryDotNet;
using Exception;
using Exception.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using Account = CloudinaryDotNet.Account;

namespace Authorization.DiExtensions;

/// <summary>
/// Dependency Injection Extension
/// </summary>
public static class DiExtension
{
    /// <summary>
    /// Initializes a test user in the system.
    /// </summary>
    /// <param name="scope">The service scope to resolve dependencies.</param>
    public static async Task InitTestUser(this IServiceScope scope)
    {
        using var baseManagerServices = scope.ServiceProvider
            .GetRequiredService<BaseManagerServices>();

        var user = await baseManagerServices.FindByIdAsync("134c2f8b-4a0f-48d2-605a-08dd64b2264b", CancellationToken.None);

        if (user == null)
        {
            await baseManagerServices.CreateAsyncByExternalIdAsync(
                new BaseUser() { Id = Guid.Parse("134c2f8b-4a0f-48d2-605a-08dd64b2264b"), UserName = "TestUser" },
                "100",
                AuthType.Base);
        }
    }
    
    /// <summary>
    /// Configures a global exception handler for the application.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    public static void AddExceptionHandler(this IApplicationBuilder app) =>
        AddExceptionHandler(app);

    /// <summary>
    /// Configures a global exception handler for the application.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    public static void AddExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run( async context =>
            {
                var activity = Activity.Current;
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;

                ProblemDetails errorDetails;

                if (exception is BaseException baseException)
                {
                    errorDetails = new ProblemDetails
                    {
                        Status = baseException.ProblemDetails.Status,
                        Title = baseException.ProblemDetails.Title,
                        Detail = baseException.ProblemDetails.Detail,
                        Type = "Authorization",
                        Instance = Environment.MachineName
                    };
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    activity?.AddException(exception);
                }
                else if (exception?.InnerException is BaseException innerException)
                {
                    errorDetails = new ProblemDetails
                    {
                        Status = innerException.ProblemDetails.Status,
                        Title = innerException.ProblemDetails.Title,
                        Detail = innerException.ProblemDetails.Detail,
                        Type = "Authorization",
                        Instance = Environment.MachineName
                    };
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    activity?.AddException(exception);
                }
                else if (exception?.InnerException is AuthenticationFailureException authenticationFailureException)
                {
                    errorDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status419AuthenticationTimeout,
                        Title = "Authentication failed.",
                        Detail = authenticationFailureException.Message,
                        Type = "Authorization",
                        Instance = Environment.MachineName
                    };
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message + " " + authenticationFailureException.Message);
                    activity?.AddException(exception);
                }
                else
                {
                    errorDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "An error occurred while processing your request.",
                        Detail = "Try again later.",
                        Type = "Authorization",
                        Instance = Environment.MachineName
                    };
                    activity?.SetStatus(ActivityStatusCode.Error, exception?.Message);
                    if(exception != null)
                        activity?.AddException(exception);
                }

                context.Response.StatusCode = errorDetails.Status;
                await context.Response.WriteAsync(errorDetails.Title + "\n" + errorDetails.Detail);
            });
        });
    }

    /// <summary>
    /// Registers required services for Identity and application.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    public static void AddServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<SecurityStampValidatorOptions>, PostConfigureSecurityStampValidatorOptions>());

        var identity = services
            .AddIdentityCore<BaseUser>()
            .AddRoles<BaseRole>()
            .AddSignInManager<BaseSignInManager<BaseUser>>()
            .AddUserStore<BaseUserStore>()
            .AddRoleStore<RoleStore>()
            .AddUserManager<BaseManagerServices>()
            .AddRoleManager<RoleManager<BaseRole>>();
        
        services.RemoveAll<IUserValidator<BaseUser>>();
        identity.AddUserValidator<BaseUserValidator>();
        
        services.RemoveAll<IRoleValidator<BaseRole>>();
        identity.AddRoleValidator<BaseRoleValidator>();

        identity.AddTokenProvider<BaseUserTwoFactorAuthentication>(Names.UserTokenProvider);
        
        services.AddCosmosClient();
        
        services.AddContainer<BaseRole>([]);
        services.AddScoped<IPersistenceStore<BaseRole, Guid>, PersistenceStore<BaseRole, Guid>>();

        services.AddContainer<BaseUser>([]);
        services.AddScoped<IPersistenceStore<BaseUser, Guid>, PersistenceStore<BaseUser, Guid>>();

        services.AddContainer<Feedback>([]);
        services.AddScoped<IPersistenceStore<Feedback, Guid>, PersistenceStore<Feedback, Guid>>();

        services.AddContainer<NotifyMe>([]);
        services.AddScoped<IPersistenceStore<NotifyMe, Guid>, PersistenceStore<NotifyMe, Guid>>();

        services.AddContainer<OAuthAuthentication>([]);
        services.AddScoped<IPersistenceStore<OAuthAuthentication, Guid>, PersistenceStore<OAuthAuthentication, Guid>>();
        
        services.AddContainer<UserToken>([], ttl: TimeSpan.FromDays(30));
        services.AddScoped<IPersistenceStore<UserToken, Guid>, PersistenceStore<UserToken, Guid>>();
        
        services.AddContainer<LinkedAccountEvent>([], 
             excludedPaths: [ new() { Path = "/Payload/*" } ], 
             uniqueKeys: [ ["/UserId", "/CorrelationId", "/Sequence" ], ["/UserId", "/Sequence"] ] );
        //services.AddContainer<LinkedAccountEvent>([]);
        services.AddScoped<IPersistenceStore<LinkedAccountEvent, Guid>, PersistenceStore<LinkedAccountEvent, Guid>>();

        services.AddScoped<ILinkedAccountEventStore, LinkedAccountEventStore>();
        services.AddScoped<IUserBaseClaimsPrincipalFactory, UserBaseClaimsPrincipalFactory>();
        services.TryAddScoped<IPasswordValidator<BaseUser>, PasswordValidator<BaseUser>>();
        services.TryAddScoped<IPasswordHasher<BaseUser>, PasswordHasher<BaseUser>>();
        services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.TryAddScoped<IRoleValidator<BaseRole>, RoleValidator<BaseRole>>();
        services.TryAddScoped<IdentityErrorDescriber>();
        services.TryAddScoped<ITwoFactorSecurityStampValidator, BaseTwoFactorSecurityStampValidator>();
        services.TryAddScoped<IUserConfirmation<BaseUser>, DefaultUserConfirmation<BaseUser>>();
        //services.TryAddScoped<BaseManagerServices>();
        //services.TryAddScoped<RoleManager<BaseRole>>();
        
        services.RemoveAll<ISecurityStampValidator>();
        services.TryAddScoped<ISecurityStampValidator, BaseUserSecurityStampValidator<BaseUser>>();

        services.TryAddScoped<IUserPlusStore, BaseUserStore>();
        services.TryAddScoped<IRoleStore<BaseRole>, RoleStore>();
        services.TryAddScoped<IRoleClaimStore<BaseRole>, RoleStore>();
    }
    
    /// <summary>
    /// Registers Cloudinary, its wrapper, and the <see cref="IAvatarService"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration with Cloudinary settings.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddAvatarService(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        var cloudinary = new Cloudinary(account) { Api = { Secure = true } };

        services.AddSingleton(cloudinary);
        services.AddSingleton<ICloudinaryWrapper, CloudinaryWrapper>();
        services.AddScoped<IAvatarService, AvatarService>();

        return services;
    }
}

internal sealed class PostConfigureSecurityStampValidatorOptions : IPostConfigureOptions<SecurityStampValidatorOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostConfigureSecurityStampValidatorOptions"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider to use.</param>
    public PostConfigureSecurityStampValidatorOptions(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }

    private TimeProvider TimeProvider { get; }

    public void PostConfigure(string? name, SecurityStampValidatorOptions options)
    {
        options.TimeProvider ??= TimeProvider;
    }
}