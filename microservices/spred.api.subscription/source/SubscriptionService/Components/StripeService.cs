using Extensions.Extensions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Abstractions;
using SubscriptionService.Configurations;
using SubscriptionService.Models;

namespace SubscriptionService.Components;

/// <inheritdoc />
public class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly SessionService _sessionService;
    private readonly Stripe.SubscriptionService _subscriptionService;
    private readonly RefundService _refundService;
    private readonly ILogger<StripeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeService"/> class using the provided configuration and Stripe client services.
    /// </summary>
    /// <param name="options">
    /// The application configuration options containing Stripe API credentials such as <see cref="StripeOptions.SecretKey"/> and <see cref="StripeOptions.PublicKey"/>.
    /// </param>
    /// <param name="sessionService">
    /// The Stripe <see cref="SessionService"/> instance used to create and manage checkout sessions.
    /// </param>
    /// <param name="subscriptionService">
    /// The Stripe <see cref="Stripe.SubscriptionService"/> instance used to manage customer subscriptions.
    /// </param>
    /// <param name="refundService">Refund service.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <remarks>
    /// During initialization, the Stripe API key is set globally via <see cref="StripeConfiguration.ApiKey"/> to enable authenticated API requests.
    /// </remarks>
    public StripeService(
        IOptions<StripeOptions> options,
        SessionService sessionService,
        Stripe.SubscriptionService subscriptionService,
        RefundService refundService,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _sessionService = sessionService;
        _subscriptionService = subscriptionService;
        _logger = loggerFactory.CreateLogger<StripeService>();
        _refundService = refundService;

        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    /// <inheritdoc />
    public async Task<string> CreateCheckoutSessionAsync(CheckoutRequest request, string email, string userId)
    {
        if (!_options.Plans.TryGetValue(request.Plan, out var priceId))
            throw new InvalidOperationException($"Invalid plan: {request.Plan}");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions { Price = priceId, Quantity = 1 }
            ],
            Mode = "subscription",
            CustomerEmail = email,
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            Metadata = new() { ["SpredUserId"] = userId }
        };

        var session = await _sessionService.CreateAsync(options);
        return session.Id;
    }
    
    /// <inheritdoc />
    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        await _subscriptionService.CancelAsync(subscriptionId);
    }
    
    /// <inheritdoc />
    public async Task TryRefundAsync(string? paymentIntentId, string reason, string userId)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            _logger.LogSpredWarning("StripeRefundMissingPaymentIntent", $"Cannot refund: missing PaymentIntent for user {userId}");
            return;
        }

        try
        {
            var refund = await _refundService.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Reason = RefundReasons.RequestedByCustomer,
                Metadata = new Dictionary<string, string>
                {
                    ["SpredUserId"] = userId,
                    ["Reason"] = reason
                }
            });

            _logger.LogSpredInformation("StripeRefundSuccess", $"Refund created for user {userId}, refundId={refund.Id}");
        }
        catch (StripeException ex)
        {
            _logger.LogSpredError("StripeRefundError", $"Refund failed for user {userId}, paymentIntent={paymentIntentId}", ex);
        }
    }
}