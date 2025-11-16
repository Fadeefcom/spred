using System.Security.Claims;
using Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Spred.Bus.Abstractions;
using Spred.Bus.Extensions;
using SubscriptionService.Abstractions;
using SubscriptionService.Models;

namespace SubscriptionService.Routes;

/// <summary>
/// Defines and maps all public subscription-related API endpoints.
/// </summary>
public static class SubscriptionRoutes
{
    /// <summary>
    /// Defines and maps all public subscription-related API endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder used to configure route mappings.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> instance to support method chaining.</returns>
    /// <remarks>
    /// This route group includes:
    /// <list type="bullet">
    /// <item>
    /// <description><c>POST /subscription/checkout</c> — Creates a Stripe checkout session for the current user.</description>
    /// </item>
    /// <item>
    /// <description><c>POST /subscription/cancel</c> — Cancels an active subscription using Stripe.</description>
    /// </item>
    /// <item>
    /// <description><c>GET /subscription/status</c> — Retrieves the current subscription status for the authenticated user.</description>
    /// </item>
    /// </list>
    /// All routes in this group require JWT-based user authorization.
    /// </remarks>
    public static IEndpointRouteBuilder MapSubscriptionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/subscription").RequireAuthorization();

        group.MapPost("/checkout", async (
            [FromBody] CheckoutRequest req,
            [FromServices] IStripeService stripeService, 
            [FromServices]  IActorProvider provider, 
            HttpRequest request) =>
        {
            var userId = provider.GetActorId();
            var email = request.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            var sessionId = await stripeService.CreateCheckoutSessionAsync(req, email, userId.ToString());
            return Results.Ok(new { sessionId });
        }).WithName("Checkout").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        group.MapPost("/cancel", async (
            [FromBody] CancelSubscriptionRequest req,
            [FromServices] IActivityWriter activityWriter,
            [FromServices] IStripeService stripeService,
            [FromServices] ISubscriptionStateStore stateStore,
            [FromServices] IActorProvider actorProvider) =>
        {
            var userId = actorProvider.GetActorId();
            var details = await stateStore.GetDetailsAsync(userId, CancellationToken.None);
            
            if(string.IsNullOrWhiteSpace(details?.SubscriptionId) || details.IsActive == false)
                return Results.BadRequest(new { error = "subscription_not_found" });
            
            await stripeService.CancelSubscriptionAsync(details.SubscriptionId);
            var id = await stateStore.SetStatusAsync(userId, string.Empty, false,
                details.SubscriptionId, req.Reason, null, null, CancellationToken.None);
            if(id.HasValue)
                await activityWriter.WriteAsync(
                    actorProvider,
                    verb: "cancel",
                    objectType: "subscription",
                    objectId: id.Value,
                    before: details,
                    after: new { status = "canceled" },
                    cancellationToken: CancellationToken.None);
            
            return Results.Ok(new { status = "canceled" });
        }).WithName("Cancel").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        group.MapGet("/status", async (
            [FromServices] ISubscriptionStateStore stateStore,
            [FromServices] IActorProvider provider) =>
        {
            var userId = provider.GetActorId();
            var status = await stateStore.GetDetailsAsync(userId);
            
            if(status != null)
                return Results.Ok(new { status.IsActive, status.CurrentPeriodStart, status.CurrentPeriodEnd, status.LogicalState });
            return Results.Ok(new { IsActive = false });
        }).WithName("Status").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
        
        group.MapPost("/refund", async (
            [FromBody] RefundRequest req,
            [FromServices] ISubscriptionStateStore stateStore,
            [FromServices] IStripeService stripeService,
            [FromServices] IActivityWriter activityWriter,
            [FromServices] IActorProvider provider) =>
        {
            var userId = provider.GetActorId();
            var details = await stateStore.GetDetailsAsync(userId, CancellationToken.None);
            if (details is null || string.IsNullOrWhiteSpace(details.PaymentId))
                return Results.BadRequest(new { error = "payment_not_found" });

            var paidAt = details.CurrentPeriodStart;
            if (!paidAt.HasValue)
                return Results.BadRequest(new { error = "paid_at_unknown" });

            var within14d = DateTime.UtcNow <= paidAt.Value.AddDays(14);
            if (!within14d)
                return Results.Conflict(new { error = "refund_window_expired" });

            await stripeService.TryRefundAsync(details.PaymentId, req.Reason ?? "user_requested_within_14d", userId.ToString());

            return Results.Ok(new { status = "refunded" });
        }).WithName("Refund").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        return app;
    }

    /// <summary>
    /// Defines and maps internal subscription-related endpoints used for Stripe webhook event handling.
    /// </summary>
    /// <param name="app">The endpoint route builder used to configure route mappings.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> instance to support method chaining.</returns>
    /// <remarks>
    /// This internal route group includes:
    /// <list type="bullet">
    /// <item>
    /// <description><c>POST /internal/stripe/webhook</c> — Processes incoming Stripe webhook events for subscription updates.</description>
    /// </item>
    /// </list>
    /// The route requires an authorization context suitable for internal service communication.
    /// </remarks>
    public static IEndpointRouteBuilder MapInternalSubscriptionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal").RequireAuthorization();

        group.MapPost("/stripe/webhook", async (
            HttpContext context,
            [FromServices] IWebhookHandler handler) =>
        {
            var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var signature = context.Request.Headers["Stripe-Signature"].ToString();
            
            if(string.IsNullOrWhiteSpace(signature))
                throw new InvalidOperationException("Invalid Stripe signature");
            
            await handler.HandleAsync(json, signature);
            return Results.Ok();
        }).AllowAnonymous();

        return app;
    }
}