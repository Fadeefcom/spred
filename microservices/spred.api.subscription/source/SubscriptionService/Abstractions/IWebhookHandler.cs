namespace SubscriptionService.Abstractions;

/// <summary>
/// Defines a contract for handling incoming Stripe webhook events.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Asynchronously processes a Stripe webhook event payload.
    /// </summary>
    /// <param name="json">The raw JSON payload received from Stripe containing event data.</param>
    /// <param name="stripeSignature">The signature header provided by Stripe used to verify the authenticity of the webhook request.</param>
    /// <returns>A task that represents the asynchronous event handling operation.</returns>
    Task HandleAsync(string json, string stripeSignature);
}
