using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Authorization.Abstractions;
using Authorization.Configuration;
using Authorization.Helpers;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Options.AuthenticationSchemes;
using Authorization.Services;
using AutoMapper;
using Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.Routes;

/// <summary>
/// Provides routes for account authentication and user-related actions.
/// </summary>
public static class AuthRoutes
{
    private static readonly HashSet<string> _validProviders = new(StringComparer.InvariantCultureIgnoreCase)
    {
        SpotifyAuthenticationDefaults.AuthenticationScheme,
        GoogleAuthenticationDefaults.AuthenticationScheme,
        YandexAuthenticationDefaults.AuthenticationScheme
    };

    /// <summary>
    /// Registers account authentication routes to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    private static void AddAuthRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/external/login", (HttpContext httpContext) =>
            {
                var provider = httpContext.Request.Query["provider"].ToString();
                var role = httpContext.Request.Query["role"].ToString();
                var deviceId = httpContext.Request.Query["deviceId"].ToString();
                var redirectMode = httpContext.Request.Query["redirect_mode"].ToString();
                if (string.IsNullOrWhiteSpace(provider)) return Results.BadRequest("provider is required");
                if (string.IsNullOrWhiteSpace(role)) return Results.BadRequest("role is required");
                if (string.IsNullOrWhiteSpace(deviceId)) return Results.BadRequest("deviceId is required");

                if (!_validProviders.TryGetValue(provider, out var authScheme))
                    return Results.BadRequest("Invalid provider");

                if (!RoleSeed.AllowedDefaults.Contains(role))
                    return Results.BadRequest("Invalid role");

                var props = new AuthenticationProperties
                {
                    RedirectUri = "/auth/login?",
                    Items =
                    {
                        ["desired_role"] = role,
                        ["device_id"] = deviceId, 
                        ["redirect_mode"] = redirectMode
                    }
                };

                return Results.Challenge(props, [authScheme]);
            })
            .WithName("Start External OAuth")
            .WithDescription("Starts OAuth challenge with desired role and device id.")
            .WithOpenApi()
            .AllowAnonymous();
        
        //Login route
        app.MapGet("/login", async (BaseManagerServices userManager,
                    HttpContext httpContext, CookieHelper cookieHelper, RedirectResponse redirectResponse,
                    CancellationToken cancellationToken) =>
            {
                var user = await userManager.FindByIdAsync(httpContext.User.Claims
                    .First(c => c.Type == ClaimTypes.NameIdentifier).Value, cancellationToken);
                if (user == null)
                    return Results.BadRequest("User not found");

                var securityToken = await userManager.GenerateUserTokenAsync(user, Names.UserTokenProvider,
                    nameof(TokenPurposes.ExternalUserToken));

                cookieHelper.AddSpredAccess(httpContext.Response.Cookies, securityToken);

                var redirectMode = httpContext.Request.Query["redirect_mode"].ToString();
                if (string.Equals(redirectMode, "same", StringComparison.OrdinalIgnoreCase))
                {
                    httpContext.Items.TryGetValue("desired_role", out var desiredRole);
                    return Results.Redirect(redirectResponse.BuildCallback(UserExtension.JustRegistered(user),
                        (desiredRole?.ToString() ?? user.UserRoles.FirstOrDefault()) ?? "Artist"));
                }
                
                if (string.Equals(redirectMode, "popup", StringComparison.OrdinalIgnoreCase))
                {
                    const string html = """
                                        <!DOCTYPE html>
                                        <html>
                                          <body>
                                            <script>
                                              window.opener?.postMessage("auth_success", "*");
                                              window.close();
                                            </script>
                                          </body>
                                        </html>
                                        """;

                    return Results.Content(html, "text/html");
                }

                return Results.NoContent();
            })
            .WithName("Login and Redirect")
            .WithDescription("Logs in the user, generates a security token, and redirects to the UI.")
            .WithOpenApi()
            .RequireAuthorization(CookieAuthenticationDefaults.AuthenticationScheme);

        // Route to log out the user
        app.MapPost("/logout", async ([FromServices] BaseSignInManager<BaseUser> signInManager)
            => await signInManager.SignOutAsync()
            ).WithName("Logout")
            .WithDescription("Logs out the authenticated user.")
            .WithOpenApi()
            .RequireAuthorization(CookieAuthenticationDefaults.AuthenticationScheme);

        // initialization headers
        app.MapGet("/init", (HttpContext context) =>
        {
            context.Response.Headers.Append("Accept-CH", "Sec-CH-UA, Sec-CH-UA-Mobile, Sec-CH-UA-Platform");
            context.Response.Headers.Append("Vary", "Sec-CH-UA, Sec-CH-UA-Mobile, Sec-CH-UA-Platform");
            return Results.NoContent();
        }).AllowAnonymous();
        
        app.MapGet("/soundcloud", async (IConfiguration configuration, HttpContext context) =>
        {
            var uiDomain = configuration["Domain:UiUrl"];
            var redirectUri = $"{uiDomain}/curator/accounts";
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var props = new AuthenticationProperties
            {
                RedirectUri = redirectUri,
                Items =
                {
                    ["link_user_id"] = userId!
                }
            };
            await context.ChallengeAsync(SoundCloudAuthenticationDefaults.AuthenticationScheme, props);
        })
        .RequireAuthorization(CookieAuthenticationDefaults.AuthenticationScheme)
        .WithName("LinkSoundCloudAccount")
        .WithOpenApi();
        
        app.MapGet("/youtube-music", async (IConfiguration configuration, HttpContext context) =>
        {
            var uiDomain = configuration["Domain:UiUrl"];
            var redirectUri = $"{uiDomain}/curator/accounts";
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
            var props = new AuthenticationProperties
            {
                RedirectUri = redirectUri,
                Items =
                {
                    ["link_user_id"] = userId!
                }
            };
            await context.ChallengeAsync(YoutubeAuthenticationDefaults.AuthenticationScheme, props);
        })
        .RequireAuthorization(CookieAuthenticationDefaults.AuthenticationScheme)
        .WithName("LinkYoutubeMusicAccount")
        .WithOpenApi();
    }
    
    /// <summary>
    /// Registers user-related routes to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    private static void AddMeRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", async (HttpContext httpContext, BaseManagerServices manager, IMapper mapper) =>
            {
                var user = await manager.GetUserAsync(httpContext.User);
                var result = mapper.Map<UserDto>(user);
                return Results.Ok(result);
            }).WithName("Me")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
        
        app.MapPatch("/me", async (UpdateUserModel userModel, HttpContext httpContext, BaseManagerServices manager, CancellationToken cancellationToken) =>
            {
                var id = manager.GetUserId(httpContext.User);
                await manager.UpdateUserByModel(id,  userModel, cancellationToken);
                return Results.NoContent();
            }).WithName("Update user info")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
        
        app.MapPut("/me/avatar", async (IFormFile file, HttpContext httpContext, [FromServices] IAvatarService avatarService, 
                [FromServices] BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var user = await manager.GetUserAsync(httpContext.User);

            if (file.Length == 0)
                return Results.BadRequest("No file uploaded");
            
            if (!file.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase))
                return Results.BadRequest("Only image files are allowed");

            await using var stream = file.OpenReadStream();
            var url = await avatarService.SaveAvatarAsync(user!.Id.ToString(), stream, file.ContentType, cancellationToken);
            if(!string.IsNullOrWhiteSpace(user.AvatarUrl))
                await avatarService.DeleteAvatarAsync(user.Id.ToString(), user.AvatarUrl, cancellationToken);
            user.AvatarUrl = url;
            await manager.UpdateAsync(user);
            
            return Results.Ok(new { message = "Avatar uploaded successfully" });
        }).WithName("Update user avatar")
        .WithOpenApi()
        .DisableAntiforgery()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        // Route for 'Notify Me' form submissions
        app.MapPost("/notify", async ([FromBody] NotifyMeFrom? appliedForm, BaseManagerServices userManager, CancellationToken cancellationToken) =>
            {
                if (appliedForm == null)
                    return Results.BadRequest("Invalid request.");
                
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(appliedForm);
                if (!Validator.TryValidateObject(appliedForm, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
                
                await userManager.AddAnonymousNotifyMe(new NotifyMe() { UserName = appliedForm.Name, 
                    Email = appliedForm.Email, ArtistType = appliedForm.ArtistType, Message = appliedForm.Message}, cancellationToken);

                return Results.Ok("Notification sent successfully.");

            })
            .WithName("Notify Me")
            .WithDescription("Submits a 'Notify Me' form with user details.")
            .WithOpenApi()
            .AllowAnonymous();

        // Route for submitting user feedback
        app.MapPost("/feedback",
                async ([FromBody] FeedbackForm? feedbackForm, BaseManagerServices userManager,
                    CancellationToken cancellationToken, HttpContext httpContext) =>
            {
                if (feedbackForm == null)
                    return Results.BadRequest("Invalid request.");

                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(feedbackForm);
                if (!Validator.TryValidateObject(feedbackForm, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
                
                var id = httpContext.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;

                await userManager.AddFeedback(
                    new Feedback()
                    { Message = feedbackForm.Message, Subject = feedbackForm.Subject, 
                        FeedbackType = feedbackForm.FeedbackType, UserId = Guid.Parse(id) },
                    cancellationToken);

                return Results.Ok("Feedback saved successfully.");
            }).WithName("Submit Feedback")
            .WithDescription("Submits user feedback.")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
    }

    /// <summary>
    /// Adds the account and user-related route groups to the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The modified endpoint route builder.</returns>
    public static IEndpointRouteBuilder AddRouteGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/auth").AddAuthRoutes();
        app.MapGroup("/user").AddMeRoutes();
        return app;
    }
}