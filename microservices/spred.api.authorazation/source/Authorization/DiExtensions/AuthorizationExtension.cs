using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Authorization.Abstractions;
using Authorization.Configuration;
using Authorization.Extensions;
using Authorization.Helpers;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Options.AuthenticationSchemes;
using Authorization.Services;
using Extensions.DiExtensions;
using Extensions.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace Authorization.DiExtensions;

/// <summary>
/// Provides extension methods for configuring authentication and authorization services
/// in an application.
/// </summary>
[SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
public static class AuthorizationExtension
{
    private static readonly TimeSpan _cookieLifetime = TimeSpan.FromDays(90);

    /// <summary>
    /// Adds application-specific authorization policies.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the policies to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddExtendedAppAuthorization(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddAppAuthorization()
            .AddPolicy("COOKIE_OR_OAUTH", policy =>
            {
                policy.AddAuthenticationSchemes("COOKIE_OR_OAUTH");
                policy.RequireAuthenticatedUser();
            })
            .AddPolicy("ManagementPolicy", policy =>
            {
                policy.AddAuthenticationSchemes("ManagementCookie");
                policy.RequireAuthenticatedUser();
            })
            .AddPolicy("SoundCloudLinkPolicy", policy =>
            {
                policy.AddAuthenticationSchemes(SoundCloudAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            })
            .AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.NameIdentifier);
                policy.RequireClaim(ClaimTypesExtension.Scheme);
                policy.RequireAuthenticatedUser();
            });

        return serviceCollection;
    }
    
    /// <summary>
    /// Configures authentication schemes for the application.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add authentication to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isDevelopment">Indicates if the environment is development.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAppAuthentication(this IServiceCollection serviceCollection,
        IConfiguration configuration, bool isDevelopment = false)
    {
        serviceCollection.ConfigureJwtSettings(configuration);
        serviceCollection.AddJwtBearer(configuration, external: true);

        serviceCollection.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie("ExternalTemp", options =>
        {
            options.Cookie.Name = "Spred.ExternalTemp";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        })
        .AddCookie(IdentityConstants.TwoFactorRememberMeScheme)
        .AddCookie("ManagementCookie", options =>
        {
            options.Cookie = new()
            {
                Name = "Spred.Management",
                Domain = ".spred.io",
                Path = "/",
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(30)
            };
            options.LoginPath = "/auth/mgmt/login";
            options.LogoutPath = "/auth/mgmt/logout";
            
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie = new()
            {
                Name = "Spred.Refresh",
                Domain = configuration["Domain:ApiDomain"],
                Path = "/auth",
                SecurePolicy = isDevelopment ? CookieSecurePolicy.None : CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = _cookieLifetime
            };

            options.LoginPath = "";
            options.LogoutPath = "";
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                },
                OnValidatePrincipal = async context =>
                {
                    if (context.HttpContext.RequestServices == null)
                    {
                        throw new InvalidOperationException("RequestServices is null.");
                    }
                    
                    ISecurityStampValidator validator = context.HttpContext.RequestServices
                        .GetRequiredService<ISecurityStampValidator>();
                    await validator.ValidateAsync(context);
                }
            };
        })
        .AddOAuth<OAuthOptions, CustomYandexHandler>(YandexAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Scope.Clear();

            IConfigurationSection section = configuration.GetSection("OAuthOption")
                .GetSection(YandexAuthenticationDefaults.AuthenticationScheme);
            options.ClientId = section["ClientId"]!;
            options.ClientSecret = section["ClientSecret"]!;
            options.CallbackPath = section["CallbackPath"]!;
            options.AuthorizationEndpoint = YandexAuthenticationDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = YandexAuthenticationDefaults.TokenEndpoint;
            options.UserInformationEndpoint = YandexAuthenticationDefaults.UserInformationEndpoint;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = false;

            options.ClaimActions.MapJsonKey("primaryId", "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "default_email");

            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{YandexAuthenticationDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };

            options.ForwardDefaultSelector = _ => YandexAuthenticationDefaults.AuthenticationScheme;

            options.Events.OnCreatingTicket = async context =>
                await TicketCreate(context, YandexAuthenticationDefaults.AuthenticationScheme, "OAuth");

            options.Events.OnTicketReceived = async context =>
                await TicketReceived(context, YandexAuthenticationDefaults.AuthenticationScheme);
        })
        .AddOAuth(SpotifyAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Scope.Clear();
            options.Scope.Add("playlist-read-private");
            options.Scope.Add("playlist-read-collaborative");
            options.Scope.Add("user-read-private");
            options.Scope.Add("user-read-email");

            options.ClaimActions.MapJsonKey("primaryId", "id");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

            var section = configuration.GetSection("OAuthOption")
                .GetSection(SpotifyAuthenticationDefaults.AuthenticationScheme);
            options.ClientId = section["ClientId"]!;
            options.ClientSecret = section["ClientSecret"]!;
            options.CallbackPath = section["CallbackPath"]!;
            options.AuthorizationEndpoint = SpotifyAuthenticationDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = SpotifyAuthenticationDefaults.TokenEndpoint;
            options.UserInformationEndpoint = SpotifyAuthenticationDefaults.UserInformationEndpoint;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = false;

            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{SpotifyAuthenticationDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };

            options.ForwardDefaultSelector = _ => SpotifyAuthenticationDefaults.AuthenticationScheme;

            options.Events.OnCreatingTicket = async context =>
                await TicketCreate(context, SpotifyAuthenticationDefaults.AuthenticationScheme, "Bearer");

            options.Events.OnTicketReceived = async context =>
                await TicketReceived(context, SpotifyAuthenticationDefaults.AuthenticationScheme);
        })
        .AddOAuth(GoogleAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Scope.Clear();
            options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
            options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
            //options.Scope.Add("https://www.googleapis.com/auth/user.addresses.read");
            //options.Scope.Add("https://www.googleapis.com/auth/user.birthday.read");
            options.Scope.Add("openid");

            options.ClaimActions.MapJsonKey("primaryId", "sub");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

            var section = configuration.GetSection("OAuthOption")
                .GetSection(GoogleAuthenticationDefaults.AuthenticationScheme);
            options.ClientId = section["ClientId"]!;
            options.ClientSecret = section["ClientSecret"]!;
            options.CallbackPath = section["CallbackPath"]!;
            options.AuthorizationEndpoint = GoogleAuthenticationDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = GoogleAuthenticationDefaults.TokenEndpoint;
            options.UserInformationEndpoint = GoogleAuthenticationDefaults.UserInformationEndpoint;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = false;

            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{GoogleAuthenticationDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };
            
            options.ForwardDefaultSelector = _ => GoogleAuthenticationDefaults.AuthenticationScheme;

            options.Events.OnCreatingTicket = async context =>
                await TicketCreate(context, GoogleAuthenticationDefaults.AuthenticationScheme, "Bearer");

            options.Events.OnTicketReceived = async context =>
                await TicketReceived(context, GoogleAuthenticationDefaults.AuthenticationScheme);
        })
        .AddOAuth(MicrosoftManagementDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = configuration["OAuthOption:MicrosoftManagement:ClientId"]!;
            options.ClientSecret = configuration["OAuthOption:MicrosoftManagement:ClientSecret"]!;
            options.CallbackPath = configuration["OAuthOption:MicrosoftManagement:CallbackPath"]!;

            options.AuthorizationEndpoint = MicrosoftManagementDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = MicrosoftManagementDefaults.TokenEndpoint;
            options.UserInformationEndpoint = MicrosoftManagementDefaults.UserInformationEndpoint;

            options.Scope.Add("User.Read");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "mail");

            options.SignInScheme = "ManagementCookie";
            options.SaveTokens = false;
            
            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{MicrosoftManagementDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };

            options.Events.OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                var response = await context.Backchannel.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                context.RunClaimActions(payload.RootElement);
            };

            options.Events.OnTicketReceived = context =>
            {
                var allowedEmails = MicrosoftManagementDefaults.AllowedEmails;

                var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrWhiteSpace(email) ||
                    !allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
                {
                    context.Fail("Unauthorized email");
                }

                return Task.CompletedTask;
            };
        })
        .AddOAuth(SoundCloudAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Scope.Clear();

            options.ClaimActions.MapJsonKey("accountId", "id");
            options.ClaimActions.MapJsonKey("accountName", "username");
            options.ClaimActions.MapJsonKey("accountEmail", "email");
            options.ClaimActions.MapCustomJson("profileUrl", user =>
            {
                if (user.TryGetProperty("permalink", out var permalinkProp) &&
                    permalinkProp.ValueKind == JsonValueKind.String)
                {
                    var permalink = permalinkProp.GetString();
                    if (!string.IsNullOrWhiteSpace(permalink))
                        return $"https://soundcloud.com/{permalink}";
                }
                return null;
            });

            var section = configuration.GetSection("OAuthOption")
                .GetSection(SoundCloudAuthenticationDefaults.AuthenticationScheme);
            options.ClientId = section["ClientId"]!;
            options.ClientSecret = section["ClientSecret"]!;
            options.CallbackPath = section["CallbackPath"]!;
            options.AuthorizationEndpoint = SoundCloudAuthenticationDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = SoundCloudAuthenticationDefaults.TokenEndpoint;
            options.UserInformationEndpoint = SoundCloudAuthenticationDefaults.UserInformationEndpoint;
            options.SignInScheme = "ExternalTemp";
            options.SaveTokens = false;

            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{SoundCloudAuthenticationDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };

            options.ForwardDefaultSelector = _ => SoundCloudAuthenticationDefaults.AuthenticationScheme;

            options.Events.OnCreatingTicket = async context =>
                await TicketCreate(context, SoundCloudAuthenticationDefaults.AuthenticationScheme, "OAuth");

            options.Events.OnTicketReceived = async context =>
                await TicketReceivedForAccount(context, SoundCloudAuthenticationDefaults.AuthenticationScheme);
        })
        .AddOAuth(YoutubeAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Scope.Clear();
            options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
            options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
            options.Scope.Add("https://www.googleapis.com/auth/youtube.readonly");
            options.Scope.Add("openid");

            options.ClaimActions.MapJsonKey("primaryId", "sub");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

            var section = configuration.GetSection("OAuthOption")
                .GetSection(YoutubeAuthenticationDefaults.AuthenticationScheme);
            options.ClientId = section["ClientId"]!;
            options.ClientSecret = section["ClientSecret"]!;
            options.CallbackPath = section["CallbackPath"]!;
            options.AuthorizationEndpoint = YoutubeAuthenticationDefaults.AuthorizationEndpoint;
            options.TokenEndpoint = YoutubeAuthenticationDefaults.TokenEndpoint;
            options.UserInformationEndpoint = YoutubeAuthenticationDefaults.UserInformationEndpoint;
            options.SignInScheme= "ExternalTemp";
            options.SaveTokens = false;

            options.CorrelationCookie = new()
            {
                Name = $"Correlation.{YoutubeAuthenticationDefaults.AuthenticationScheme}.",
                Path = "/auth",
                Domain = configuration["Domain:ApiDomain"],
                SecurePolicy = CookieSecurePolicy.Always,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
            };
            
            options.ForwardDefaultSelector = _ => YoutubeAuthenticationDefaults.AuthenticationScheme;

            options.Events.OnCreatingTicket = async context =>
            {
                await TicketCreate(context, YoutubeAuthenticationDefaults.AuthenticationScheme, "Bearer");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", context.AccessToken);

                var response = await client.GetAsync("https://www.googleapis.com/youtube/v3/channels?part=id&mine=true");
                if (!response.IsSuccessStatusCode)
                {
                    context.Fail("Failed to fetch YouTube account information");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                var channel = parsed["items"]?.FirstOrDefault();
                var channelId = channel?["id"]?.ToString();
                var channelName = channel?["snippet"]?["title"]?.ToString();

                if (!string.IsNullOrWhiteSpace(channelId))
                {
                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    identity.AddClaim(new Claim("accountId", channelId));
                    identity.AddClaim(new Claim("accountName", channelName ?? channelId));
                    identity.AddClaim(new Claim("profileUrl", $"https://music.youtube.com/channel/{channelId}"));
                }
            };

            options.Events.OnTicketReceived = async context =>
                await TicketReceivedForAccount(context, YoutubeAuthenticationDefaults.AuthenticationScheme);
            
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var uri = context.RedirectUri + "&access_type=offline&prompt=consent";
                context.Response.Redirect(uri);
                return Task.CompletedTask;
            };
        })
        .AddPolicyScheme("COOKIE_OR_OAUTH", "COOKIE_OR_OAUTH", option =>
        {
            option.ForwardDefaultSelector = context =>
            {
                var authScheme = context.Request.Query["authType"].FirstOrDefault()?.ToLowerInvariant() switch
                {
                    "spotify" => SpotifyAuthenticationDefaults
                        .AuthenticationScheme,
                    "yandex" => YandexAuthenticationDefaults
                        .AuthenticationScheme,
                    "google" => GoogleAuthenticationDefaults
                        .AuthenticationScheme,
                    _ => CookieAuthenticationDefaults.AuthenticationScheme,
                };
                
                //var deviceId = context.Request.Query["deviceId"].FirstOrDefault();
                //context.Items.TryAdd(ClaimTypesExtension.DeviceId, deviceId);
                return authScheme;
            };
        });

        return serviceCollection;
    }
    
    /// <summary>
    /// Creates an authentication ticket for OAuth providers.
    /// </summary>
    /// <param name="context">The OAuth creating ticket context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="authScheme">The authorization scheme used.</param>
    private static async Task TicketCreate(OAuthCreatingTicketContext context, string scheme, string authScheme)
    {
        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("OAuth");
        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue(authScheme, context.AccessToken);
        
        logger.LogSpredInformation("TicketCreate", "AccessToken used");
        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occurred when retrieving {scheme} user information ({response.StatusCode}).");
        }

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
        
        context.RunClaimActions(payload.RootElement);
        context.Identity!.AddClaim(new Claim(ClaimTypesExtension.Scheme, scheme));
        context.HttpContext.Items.Add("AccessToken", context.AccessToken);
        context.HttpContext.Items.Add("RefreshToken", context.RefreshToken);
    }

    /// <summary>
    /// Handles post-authentication logic after receiving an OAuth ticket.
    /// </summary>
    /// <param name="context">The ticket received context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    private static async Task TicketReceived(TicketReceivedContext context, string scheme)
    {
        var baseManagerServices = context.HttpContext.RequestServices.GetRequiredService<BaseManagerServices>();
        var primaryId = context.Principal!.Claims.FirstOrDefault(c => c.Type == "primaryId")?.Value;
        var userName = context.Principal!.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var email = context.Principal!.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(primaryId) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email))
        {
            context.Fail("id,email or userName is empty");
            return;
        }

        // var deviceFromProps = context.Properties?.Items.TryGetValue("device_id", out var dev) == true ? dev : null;
        // if (string.IsNullOrWhiteSpace(deviceFromProps))
        // {
        //     context.Fail("DeviceId mismatch");
        //     return;
        // }

        var user = await baseManagerServices.FindByPrimaryId(primaryId, TokenPurposeHelper.GetAuthType(context.Scheme.Name)!.Value);
        var createdNow = false;
        if (user == null)
        {
            user = new BaseUser
            {
                UserName = userName, Email = email,
                UserClaims =
                {
                    ["scope"] = []
                }
            };
            var result = await baseManagerServices.CreateAsyncByExternalIdAsync(user, primaryId, TokenPurposeHelper.GetAuthType(context.Scheme.Name)!.Value);
            if (!result.Succeeded)
            {
                context.Fail(result.Errors.First().Description);
                return;
            }
            createdNow = true;

            var accessToken = context.HttpContext.Items.FirstOrDefault(i => i.Key.ToString() == "AccessToken").Value?.ToString() ?? string.Empty;
            var refreshToken = context.HttpContext.Items.FirstOrDefault(i => i.Key.ToString() == "RefreshToken").Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(accessToken)) 
                await baseManagerServices.SetAuthenticationTokenAsync(user, context.Scheme.Name, "AccessToken", accessToken);
            if (!string.IsNullOrWhiteSpace(refreshToken)) 
                await baseManagerServices.SetAuthenticationTokenAsync(user, context.Scheme.Name, "RefreshToken", refreshToken);
        }

        if (createdNow && context.Properties?.Items.TryGetValue("desired_role", out var desiredRole) == true)
        {
            if (RoleSeed.AllowedDefaults.TryGetValue(desiredRole ?? string.Empty, out var role) && !string.IsNullOrWhiteSpace(role))
            {
                user = await baseManagerServices.FindByIdAsync(user.Id.ToString());
                // update user happened here
                await baseManagerServices.AddToRoleAsync(user!, role);
            }
        }

        if (context.Principal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user!.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Sid, user.SecurityStamp!));
            identity.AddClaim(new Claim(ClaimTypesExtension.Scheme, scheme));
        }
        
        var redirectMode = context.Properties?.Items.TryGetValue("redirect_mode", out var rm) == true && !string.IsNullOrWhiteSpace(rm)
            ? rm
            : "popup";
        
        var redirectUri = QueryHelpers.AddQueryString("/auth/login", new Dictionary<string, string?>
        {
            ["redirect_mode"] = redirectMode
        });

        context.ReturnUri = redirectUri;
    }
    
    private static async Task TicketReceivedForAccount(TicketReceivedContext context, string scheme)
    {
        var baseManagerServices = context.HttpContext.RequestServices.GetRequiredService<BaseManagerServices>();
        var linkedAccountService = context.HttpContext.RequestServices.GetRequiredService<ILinkedAccountEventStore>();

        var accountId = context.Principal!.Claims.FirstOrDefault(c => c.Type == "accountId")?.Value;
        var accountName = context.Principal!.Claims.FirstOrDefault(c => c.Type == "accountName")?.Value;
        var userEmail = context.Principal!.Claims.FirstOrDefault(c => c.Type == "accountEmail")?.Value;
        var profileUrl = context.Principal!.Claims.FirstOrDefault(c => c.Type == "profileUrl")?.Value;

        if (string.IsNullOrWhiteSpace(accountId))
        {
            context.Fail("id is empty");
            return;
        }
        
        var userId = context.Properties?.Items.TryGetValue("link_user_id", out var v) == true ? v : null;
        var user = await baseManagerServices.FindByIdAsync(userId!);
        if (user == null)
        {
            context.Fail("User must be authenticated before linking SoundCloud account");
            return;
        }

        var payload = new JObject
        {
            ["accountId"] = accountId,
            ["userName"] = accountName,
            ["email"] = userEmail,
            ["linkedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ["accessToken"] = context.HttpContext.Items["AccessToken"]?.ToString() ?? string.Empty,
            ["refreshToken"] = context.HttpContext.Items["RefreshToken"]?.ToString() ?? string.Empty
        };
        var platform = AccountPlatformHelper.PlatformSchemeMap[scheme];
        
        var linkedAccount = await linkedAccountService.AppendAsync(accountId:accountId, userId: user.Id,
            platform: platform, LinkedAccountEventType.AccountLinked, payload: payload, CancellationToken.None);
        if (linkedAccount.Succeeded)
        {
            user.UserAccounts.Add(new UserAccountRef(platform, accountId, profileUrl ?? string.Empty));
            await baseManagerServices.UpdateAsync(user);
        }

        context.Success();
    }
}