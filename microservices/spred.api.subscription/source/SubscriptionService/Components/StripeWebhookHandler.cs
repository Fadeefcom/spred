using Extensions.Extensions;
using Microsoft.Extensions.Options;
using Spred.Bus.Abstractions;
using Spred.Bus.Contracts;
using Spred.Bus.Extensions;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Abstractions;
using SubscriptionService.Configurations;
using SubscriptionService.Models.Entities;

namespace SubscriptionService.Components;

/// <summary>
/// Processes Stripe webhook callbacks and translates them into internal subscription state changes.
/// </summary>
/// <remarks>
/// <para><b>Security</b>: Verifies the request signature via <see cref="Stripe.EventUtility.ConstructEvent(string,string,string, long, bool)"/>
/// using the configured <c>WebhookSecret</c>. If verification fails, an <see cref="UnauthorizedAccessException"/> is thrown.</para>
/// <para><b>Idempotency</b>: Stripe may deliver events more than once. Handlers must be side-effect safe:
/// update state using upserts and store raw snapshots for replay/reconciliation.</para>
/// <para><b>Handled event types</b> (non-exhaustive):
/// <list type="bullet">
/// <item><description><c>checkout.session.completed</c>, <c>checkout.session.async_payment_succeeded</c></description></item>
/// <item><description><c>checkout.session.async_payment_failed</c>, <c>checkout.session.expired</c></description></item>
/// <item><description><c>customer.subscription.created</c>, <c>customer.subscription.updated</c>, <c>customer.subscription.deleted</c></description></item>
/// <item><description><c>customer.subscription.trial_will_end</c>, <c>customer.subscription.pending_update_applied</c>, <c>customer.subscription.pending_update_expired</c></description></item>
/// <item><description><c>invoice.finalized</c>, <c>invoice.finalization_failed</c>, <c>invoice.payment_succeeded</c>, <c>invoice.payment_failed</c></description></item>
/// <item><description><c>payment_intent.succeeded</c>, <c>payment_intent.payment_failed</c>, <c>payment_intent.canceled</c></description></item>
/// <item><description><c>charge.refunded</c>, <c>refund.updated</c></description></item>
/// </list>
/// </para>
/// </remarks>
public class StripeWebhookHandler : IWebhookHandler
{
    private readonly StripeOptions _options;
    private readonly ISubscriptionStateStore _stateStore;
    private readonly IStripeService _stripeService;
    private readonly Stripe.SubscriptionService _subscriptionService;
    private readonly SessionLineItemService _sessionLineItemService;
    private readonly IActivityWriter _activityWriter;
    private readonly IActorProvider _actorProvider;
    private readonly ILogger<StripeWebhookHandler> _logger;
    private readonly InvoiceService _invoiceService;

    private static readonly string[] _stripeCheckoutTags = ["stripe", "checkout"];
    private static readonly string[] _stripeSubscriptionTags = ["stripe", "subscription"];

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookHandler"/> class.
    /// </summary>
    /// <param name="options">Application options containing Stripe settings (e.g., webhook secret).</param>
    /// <param name="stateStore">Abstraction for persisting and reading subscription state and snapshots.</param>
    /// <param name="loggerFactory">Factory for creating a typed <see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="activityWriter">Writer for emitting high-level domain activities (used only for create/cancel events).</param>
    /// <param name="actorProvider">Provides the current actor context for audit trails.</param>
    /// <param name="sessionLineItemService">Stripe service for reading checkout session line items.</param>
    /// <param name="stripeService">Domain service wrapper for Stripe operations (e.g., refunds).</param>
    /// <param name="subscriptionService">Stripe SDK service used to fetch subscription details.</param>
    /// <param name="invoiceService">Stripe SDK service used to resolve invoices.</param>
    /// <remarks>
    /// This handler does not mutate Stripe state directly; it only reads Stripe entities and writes internal state.
    /// All Stripe mutations (refunds, cancellations) must go through <paramref name="stripeService"/>.
    /// </remarks>
    public StripeWebhookHandler(
        IOptions<StripeOptions> options,
        ISubscriptionStateStore stateStore,
        ILoggerFactory loggerFactory,
        IActivityWriter activityWriter,
        IActorProvider actorProvider,
        SessionLineItemService sessionLineItemService,
        IStripeService stripeService,
        Stripe.SubscriptionService subscriptionService,
        InvoiceService invoiceService)
    {
        _options = options.Value;
        _stateStore = stateStore;
        _logger = loggerFactory.CreateLogger<StripeWebhookHandler>();
        _activityWriter = activityWriter;
        _actorProvider = actorProvider;
        _sessionLineItemService = sessionLineItemService;
        _stripeService = stripeService;
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Verifies, parses, and routes a Stripe webhook to a specific handler based on <c>event.type</c>.
    /// </summary>
    /// <param name="json">The raw JSON payload received from Stripe.</param>
    /// <param name="stripeSignature">The value of the <c>Stripe-Signature</c> header used to verify authenticity.</param>
    /// <returns>A task that completes when the event has been processed.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when signature verification fails (invalid or missing signature, timestamp outside tolerance, or secret mismatch).
    /// </exception>
    /// <remarks>
    /// <para>Parsing uses <see cref="Stripe.EventUtility.ConstructEvent(string,string,string, long, bool)"/> with the configured secret.</para>
    /// <para>On success, dispatches to the corresponding <c>Handle*</c> method (e.g., <c>HandleInvoiceEventAsync</c>).</para>
    /// <para>Handlers must be resilient to duplicate deliveries and to partial Stripe data (e.g., missing metadata).</para>
    /// </remarks>
    public async Task HandleAsync(string json, string stripeSignature)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogSpredWarning("StripeWebhookInvalidSignature", "Invalid Stripe signature detected.", ex);
            throw new UnauthorizedAccessException("Invalid Stripe signature", ex);
        }

        _logger.LogSpredInformation("StripeWebhookReceived", $"Received Stripe event: {stripeEvent.Type}");

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            case "checkout.session.async_payment_succeeded":
                await HandleCheckoutSessionCompletedAsync((Session)stripeEvent.Data.Object);
                break;

            case "checkout.session.async_payment_failed":
            case "checkout.session.expired":
                await HandleCheckoutSessionNonCompletedAsync((Session)stripeEvent.Data.Object, stripeEvent.Type);
                break;

            case "customer.subscription.created":
                await HandleSubscriptionCreatedOrUpdatedAsync((Subscription)stripeEvent.Data.Object, "created");
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionCreatedOrUpdatedAsync((Subscription)stripeEvent.Data.Object, "updated");
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync((Subscription)stripeEvent.Data.Object);
                break;

            case "customer.subscription.trial_will_end":
            case "customer.subscription.pending_update_applied":
            case "customer.subscription.pending_update_expired":
                await HandleSubscriptionMetaEventAsync((Subscription)stripeEvent.Data.Object, stripeEvent.Type);
                break;

            case "invoice.finalized":
            case "invoice.finalization_failed":
            case "invoice.payment_succeeded":
            case "invoice.payment_failed":
                await HandleInvoiceEventAsync((Invoice)stripeEvent.Data.Object, stripeEvent.Type);
                break;

            case "payment_intent.succeeded":
            case "payment_intent.payment_failed":
            case "payment_intent.canceled":
                await HandlePaymentIntentEventAsync((PaymentIntent)stripeEvent.Data.Object, stripeEvent.Type);
                break;

            case "charge.refunded":
                await HandleChargeRefundedAsync((Charge)stripeEvent.Data.Object);
                break;

            case "refund.created":
            case "refund.updated":
                await HandleRefundUpdatedAsync((Refund)stripeEvent.Data.Object);
                break;

            default:
                _logger.LogSpredInformation("StripeWebhookUnhandledEvent", $"Unhandled Stripe event: {stripeEvent.Type}");
                break;
        }
    }

    private static (bool isActive, string logicalState) MapStripeStatus(Subscription? s, bool allowTrialAccess = true)
    {
        if (s == null)
            return (false, "unknown");
        
        return s.Status switch
        {
            "active" => (true, "active"),
            "trialing" => (allowTrialAccess, allowTrialAccess ? "trialing" : "trial_blocked"),
            "past_due" => (true, "past_due"),
            "incomplete" => (false, "incomplete"),
            "incomplete_expired" => (false, "incomplete_expired"),
            "unpaid" => (false, "unpaid"),
            "canceled" => (false, "canceled"),
            "paused" => (false, "paused"),
            _ => (false, s.Status)
        };
    }

    private static (DateTime? start, DateTime? end) ComputePeriod(Subscription? s)
    {
        if (s == null)
        {
            return (DateTime.Now, DateTime.Now.AddDays(30));
        }
        
        var start = s.BillingCycleAnchor;
        DateTime? end = start;
        var recurring = s.Items?.Data?.FirstOrDefault()?.Price?.Recurring;
        if (recurring != null)
        {
            var count = (int)recurring.IntervalCount;
            end = recurring.Interval switch
            {
                "day" => start.AddDays(count),
                "week" => start.AddDays(7 * count),
                "month" => start.AddMonths(count),
                "year" => start.AddYears(count),
                _ => start
            };
        }
        return (start, end);
    }

    private static (DateTime? start, DateTime? end) ComputePeriodFromInvoice(Invoice inv)
        => (inv.PeriodStart, inv.PeriodEnd);
    
    private async Task<string?> ResolvePaymentIntentIdFromInvoiceAsync(string invoiceId)
    {
        if(string.IsNullOrWhiteSpace(invoiceId)) 
            return null;
        
        var invoice = await _invoiceService.GetAsync(invoiceId);
        var paymentId = invoice?.Payments?.Data?
            .OrderByDescending(p => p?.Created ?? DateTime.MinValue)
            .Select(p => p?.Payment?.PaymentIntentId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        
        return string.IsNullOrWhiteSpace(paymentId) ? null : paymentId;
    }

    private async Task<string?> ResolvePaymentIntentIdFromSubscriptionAsync(string subscriptionId)
    {
        if(string.IsNullOrWhiteSpace(subscriptionId)) 
            return null;
        
        var sub = await _subscriptionService.GetAsync(subscriptionId);
        var invoiceId = sub?.LatestInvoiceId;
        if (string.IsNullOrWhiteSpace(invoiceId)) return null;
        return await ResolvePaymentIntentIdFromInvoiceAsync(invoiceId);
    }

    private async Task HandleCheckoutSessionCompletedAsync(Session session)
    {
        var subscriptionId = session.Subscription?.Id ?? session.SubscriptionId;
        string? paymentId = null;
        
        if (string.IsNullOrWhiteSpace(paymentId))
            paymentId = session.PaymentIntentId;
        
        if (!string.IsNullOrWhiteSpace(subscriptionId) && string.IsNullOrWhiteSpace(paymentId))
            paymentId = await ResolvePaymentIntentIdFromSubscriptionAsync(subscriptionId);

        if (!session.Metadata.TryGetValue("SpredUserId", out var userIdRaw))
        {
            _logger.LogSpredWarning("StripeWebhookMissingMetadata", "Missing SpredUserId in checkout.session.completed event.");
            await _stripeService.TryRefundAsync(paymentId, "missing_spred_user_id", "unknown");
            return;
        }

        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            _logger.LogSpredWarning("StripeWebhookInvalidUserId", $"Invalid SpredUserId format in checkout.session.completed: {userIdRaw}");
            await _stripeService.TryRefundAsync(paymentId, "invalid_spred_user_id", userIdRaw);
            return;
        }
        
        Subscription? sub = null;
        if (!string.IsNullOrWhiteSpace(subscriptionId))
            sub = await _subscriptionService.GetAsync(subscriptionId);

        var lineItems = await _sessionLineItemService.ListAsync(session.Id, new SessionLineItemListOptions { Limit = 10 });
        var planName = lineItems.FirstOrDefault()?.Price?.Nickname ?? lineItems.FirstOrDefault()?.Description ?? "unknown";
        var totalAmount = lineItems.Sum(i => (i.AmountTotal) / 100.0);
        
        var (start, end) = ComputePeriod(sub ?? session.Subscription);
        var (isActiveByStatus, logicalState) = MapStripeStatus(sub ?? session.Subscription, allowTrialAccess: true);

        var subscription = new UserSubscriptionStatus()
        {
            UserId = userId,
            SubscriptionId = subscriptionId ?? string.Empty,
            IsActive = isActiveByStatus,
            PaymentId = paymentId ?? string.Empty,
            LogicalState = logicalState,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = end
        };
        
        var result = await _stateStore.SaveAtomicAsync(userId, subscription, "checkout_session:completed", 
            session.Id, session.ToJson(), CancellationToken.None);

        if (!result.SnapshotSaved || !result.StatusSaved)
        {
            _logger.LogSpredWarning("SubscriptionStateError", $"Failed to persist subscription state for user {userId}, refunding");
            await _stripeService.TryRefundAsync(paymentId, "subscription_state_error", userIdRaw);
            return;
        }

        await _activityWriter.WriteAsync(
            _actorProvider,
            verb: "subscribed",
            objectType: "subscription",
            objectId: subscription.Id,
            messageKey: "subscription.created",
            ownerUserId: userId,
            messageArgs: new Dictionary<string, object?>
            {
                ["plan"] = planName,
                ["amount"] = totalAmount
            },
            service: "SubscriptionService",
            importance: ActivityImportance.Important,
            tags: _stripeCheckoutTags);

        _logger.LogSpredInformation("StripeWebhookCheckoutCompleted", $"Activated subscription for user {userId} - {subscription.Id} (period {start:yyyy-MM-dd} → {end:yyyy-MM-dd})");
    }

    private async Task HandleCheckoutSessionNonCompletedAsync(Session session, string type)
    {
        var userIdRaw = session.Metadata.GetValueOrDefault("SpredUserId");
        var userId = Guid.TryParse(userIdRaw, out var g) ? g : Guid.Empty;
        var details = await _stateStore.GetDetailsAsync(userId, CancellationToken.None);

        await _stateStore.SaveSnapshotAsync(
            userId,
            details?.Id ?? Guid.Empty,
            kind: $"checkout_session:{type}",
            id: session.Id,
            rawJson: session.ToJson(),
            CancellationToken.None);
    }

    private async Task HandleSubscriptionCreatedOrUpdatedAsync(Subscription s, string source)
    {
        if (!s.Metadata.TryGetValue("SpredUserId", out var userRaw) || !Guid.TryParse(userRaw, out var userId))
            userId = Guid.Empty;

        var (start, end) = ComputePeriod(s);
        var (isActiveByStatus, logicalState) = MapStripeStatus(s, allowTrialAccess: true);
        var paymentId = await ResolvePaymentIntentIdFromInvoiceAsync(s.LatestInvoiceId);
        
        var subscription = new UserSubscriptionStatus()
        {
            UserId = userId,
            SubscriptionId = s.Id,
            IsActive = isActiveByStatus,
            PaymentId = paymentId ?? string.Empty,
            LogicalState = logicalState,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = end
        };
        
        var result = await _stateStore.SaveAtomicAsync(userId, subscription, $"subscription:{source}:{s.Status}", 
            s.Id, s.ToJson(), CancellationToken.None);

        if (result.SnapshotSaved && result.StatusSaved && (source == "updated" && s.Status == "canceled"))
        {
            await _activityWriter.WriteAsync(
                _actorProvider,
                verb: "canceled",
                objectType: "subscription",
                objectId: subscription.Id,
                messageKey: "subscription.canceled",
                ownerUserId: userId,
                service: "SubscriptionService",
                importance: ActivityImportance.Important,
                after: new {logicalState, start, end},
                tags: _stripeSubscriptionTags);
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Subscription deleted)
    {
        var userId = (deleted.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;
        var (isActiveByStatus, logicalState) = MapStripeStatus(deleted, allowTrialAccess: false);
        
        var subscription = new UserSubscriptionStatus()
        {
            UserId = userId,
            SubscriptionId = deleted.Id,
            IsActive = isActiveByStatus,
            PaymentId = string.Empty,
            LogicalState = logicalState,
            CurrentPeriodStart = null,
            CurrentPeriodEnd = null
        };
        
        var result = await _stateStore.SaveAtomicAsync(userId, subscription, "subscription:deleted", 
            deleted.Id, deleted.ToJson(), CancellationToken.None);

        if (result.SnapshotSaved && result.StatusSaved)
        {
            await _activityWriter.WriteAsync(
                _actorProvider,
                verb: "canceled",
                objectType: "subscription",
                objectId: subscription.Id,
                messageKey: "subscription.canceled",
                ownerUserId: userId,
                service: "SubscriptionService",
                importance: ActivityImportance.Important,
                tags: _stripeSubscriptionTags);
        }
    }

    private async Task HandleSubscriptionMetaEventAsync(Subscription s, string type)
    {
        var userId = (s.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;
        var details = await _stateStore.GetDetailsAsync(userId, CancellationToken.None);

        await _stateStore.SaveSnapshotAsync(
            userId,
            details?.Id ?? Guid.Empty,
            kind: $"subscription_event:{type}",
            id: s.Id,
            rawJson: s.ToJson(),
            CancellationToken.None);
    }

    private async Task HandleInvoiceEventAsync(Invoice inv, string type)
    {
        var userId = (inv.Metadata != null && inv.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;

        if (type == "invoice.payment_succeeded" && userId != Guid.Empty)
        {
            var (start, end) = ComputePeriodFromInvoice(inv);

            var paymentId = await ResolvePaymentIntentIdFromInvoiceAsync(inv.Id) ?? string.Empty;
            
            var subscriptionId = string.Empty;
            if (inv.Metadata != null && inv.Metadata.TryGetValue("SubscriptionId", out var sid) && !string.IsNullOrWhiteSpace(sid))
                subscriptionId = sid;
            
            var subscription = new UserSubscriptionStatus()
            {
                UserId = userId,
                SubscriptionId = subscriptionId,
                IsActive = true,
                PaymentId = paymentId,
                LogicalState = "payment succeeded",
                CurrentPeriodStart = start,
                CurrentPeriodEnd = end
            };
            
            await _stateStore.SaveAtomicAsync(userId, subscription, $"invoice:{type}", 
                inv.Id, inv.ToJson(), CancellationToken.None);
        }
        else
        {
            await _stateStore.SaveSnapshotAsync(
                userId,
                Guid.Empty,
                kind: $"invoice:{type}",
                id: inv.Id,
                rawJson: inv.ToJson(),
                CancellationToken.None);
        }
    }

    private async Task HandlePaymentIntentEventAsync(PaymentIntent pi, string type)
    {
        var userId = (pi.Metadata != null && pi.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;

        var status = await _stateStore.GetDetailsAsync(userId, CancellationToken.None);
        
        await _stateStore.SaveSnapshotAsync(
            userId,
            status?.Id ?? Guid.Empty,
            kind: $"payment_intent:{type}",
            id: pi.Id,
            rawJson: pi.ToJson(),
            CancellationToken.None);
    }

    private async Task HandleChargeRefundedAsync(Charge charge)
    {
        var userId = (charge.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;
        
        var subscription = new UserSubscriptionStatus()
        {
            UserId = userId,
            SubscriptionId = string.Empty,
            IsActive = false,
            PaymentId = charge.PaymentIntentId ?? string.Empty,
            LogicalState = "charge refunded",
            CurrentPeriodStart = null,
            CurrentPeriodEnd = null
        };
        
        var result = await _stateStore.SaveAtomicAsync(userId, subscription, "charge:refunded", 
            charge.Id, charge.ToJson(), CancellationToken.None);

        if (result.SnapshotSaved && result.StatusSaved)
        {
            await _activityWriter.WriteAsync(
                _actorProvider,
                verb: "canceled",
                objectType: "subscription",
                objectId: subscription.Id,
                messageKey: "subscription.canceled",
                ownerUserId: userId,
                service: "SubscriptionService",
                importance: ActivityImportance.Important,
                tags: _stripeSubscriptionTags);
        }
    }

    private async Task HandleRefundUpdatedAsync(Refund refund)
    {
        var userId = (refund.Metadata != null && refund.Metadata.TryGetValue("SpredUserId", out var raw) && Guid.TryParse(raw, out var g)) ? g : Guid.Empty;
        var details = await _stateStore.GetDetailsAsync(userId, CancellationToken.None);
        
        await _stateStore.SaveSnapshotAsync(
            userId,
            details?.Id ?? Guid.Empty,
            kind: "refund:updated",
            id: refund.Id,
            rawJson: refund.ToJson(),
            CancellationToken.None);
    }
}
