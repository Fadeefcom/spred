using System.Security.Claims;
using Authorization.Models.Dto;
using Authorization.Services;
using Extensions.Configuration;

namespace Authorization.Routes;

/// <summary>
/// Provides the routes related to account management functionality in the application.
/// </summary>
public static class AccountRoutes
{
    private static void MapAccountsRoutes(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("", async (HttpContext context, BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var userId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            var accounts = await manager.GetUserAccountsAsync(Guid.Parse(userId), cancellationToken);
            return Results.Ok(accounts);
        })
        .WithName("Get Accounts")
        .WithOpenApi()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        routes.MapPost("", async (CreateAccountRequest request, HttpContext context, BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var userId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            
            var created = await manager.AddAccountAsync(Guid.Parse(userId), request, cancellationToken);
            return created.isCreated
                ? Results.Created($"/accounts/{created.accountId}", new { created.accountId })
                : Results.BadRequest(new { status = created.message});
        })
        .WithName("Add Account")
        .WithOpenApi()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        routes.MapPost("/{accountId}/token", async (string accountId, HttpContext context, BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var userId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            
            var ok = await manager.GetTokenVerification(Guid.Parse(userId), accountId, cancellationToken);
            return ok.isCreated ?
                Results.Ok(new { tokeen = ok.token }) : 
                Results.BadRequest(new { accountId, status = "Unable to create verification token. Please try again." });
        })
        .WithName("Get Account token")
        .WithOpenApi()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
        
        routes.MapPost("/{accountId}/verify", async (string accountId, HttpContext context, BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var userId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            
            var ok = await manager.StartVerifyAccountAsync(Guid.Parse(userId), accountId, cancellationToken);
            return ok.Item1 ?
                Results.Accepted() : 
                Results.BadRequest(new { accountId, status = ok.Item2 });
        })
        .WithName("Verify Account")
        .WithOpenApi()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        routes.MapDelete("/{accountId}", async (string accountId, HttpContext context, BaseManagerServices manager, CancellationToken cancellationToken) =>
        {
            var userId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            
            var ok = await manager.DeleteAccountAsync(Guid.Parse(userId), accountId, cancellationToken);
            return ok ? Results.NoContent() : Results.BadRequest(new { accountId, status = "error" });
        })
        .WithName("Delete Account")
        .WithOpenApi()
        .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
    }

    /// <summary>
    /// Adds the route group for account-related endpoints to the application's endpoint routing.
    /// </summary>
    public static IEndpointRouteBuilder AddAccountRouteGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/user/accounts").MapAccountsRoutes();
        return app;
    }
}