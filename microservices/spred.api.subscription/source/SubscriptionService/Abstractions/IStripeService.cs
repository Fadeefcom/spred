using SubscriptionService.Models;

namespace SubscriptionService.Abstractions;

/// <summary>
/// Defines methods for managing Stripe-based subscription operations such as checkout session creation and subscription cancellation.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Creates a new Stripe checkout session for the specified user and course plan.
    /// </summary>
    /// <param name="request">The checkout request containing plan and pricing details.</param>
    /// <param name="email">The user's email address associated with the Stripe customer.</param>
    /// <param name="userId">The unique identifier of the user initiating the checkout session.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the identifier of the created Stripe checkout session.
    /// </returns>
    Task<string> CreateCheckoutSessionAsync(CheckoutRequest request, string email, string userId);

    /// <summary>
    /// Cancels an existing Stripe subscription.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to cancel.</param>
    /// <returns>A task that represents the asynchronous cancellation operation.</returns>
    Task CancelSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Attempts to issue a refund for the specified payment intent, providing a reason for the refund.
    /// </summary>
    /// <param name="paymentIntentId">The unique identifier of the payment intent to refund. This may be null if not applicable.</param>
    /// <param name="reason">The reason for initiating the refund.</param>
    /// <param name="userId">The unique identifier of the user requesting the refund.</param>
    /// <returns>
    /// A task that represents the asynchronous refund operation. The task result indicates whether the refund attempt was successful.
    /// </returns>
    Task TryRefundAsync(string? paymentIntentId, string reason, string userId);
}