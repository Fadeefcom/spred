using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Spred.Bus.Abstractions;
using Spred.Bus.Contracts;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Abstractions;
using SubscriptionService.Components;
using SubscriptionService.Configurations;
using SubscriptionService.Models;
using SubscriptionService.Models.Entities;
using Xunit;

namespace SubscriptionService.Test;

public class StripeWebhookHandlerTests
{
    private readonly string _secret = "whsec_test_secret";
    private readonly Mock<ISubscriptionStateStore> _stateStore = new();
    private readonly Mock<IStripeService> _stripeService = new();
    private readonly Mock<Stripe.SubscriptionService> _stripeSub = new(MockBehavior.Strict, (StripeClient?)null);
    private readonly Mock<SessionLineItemService> _lineItems = new(MockBehavior.Strict, (StripeClient?)null);
    private readonly Mock<InvoiceService> _invoiceService = new(MockBehavior.Strict, (StripeClient?)null);
    private readonly Mock<IActivityWriter> _activity = new();
    private readonly Mock<IActorProvider> _actor = new();
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.AddDebug());

    private static string WrapEvent(string type, string objectJson)
        => $"{{\"id\":\"evt_test\",\"object\":\"event\",\"api_version\":\"2025-09-30.clover\",\"created\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()},\"livemode\":false,\"pending_webhooks\":1,\"request\":{{\"id\":\"req_test\",\"idempotency_key\":null}},\"type\":\"{type}\",\"data\":{{\"object\":{objectJson},\"previous_attributes\":{{}}}}}}";

    private StripeWebhookHandler CreateHandler()
    {
        var options = Options.Create(new StripeOptions { WebhookSecret = _secret });
        return new StripeWebhookHandler(options, _stateStore.Object, _loggerFactory, _activity.Object, _actor.Object, _lineItems.Object, _stripeService.Object, _stripeSub.Object, _invoiceService.Object);
    }

    public StripeWebhookHandlerTests()
    {
        _actor.Setup(a => a.GetActorId()).Returns(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        _actor.Setup(a => a.GetCorrelationId()).Returns("test-correlation-id");
        _actor.Setup(a => a.GetRoleName()).Returns("test-role");
    }

    private static string Sign(string secret, string json, long? ts = null)
    {
        var timestamp = ts ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", string.Empty).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    [Fact]
    public async Task CheckoutCompleted_WithMetadata_SavesStateAndActivity()
    {
        var obj = "{\"id\":\"cs_test_1\",\"object\":\"checkout.session\",\"payment_status\":\"paid\",\"subscription\":{\"id\":\"sub_123\"},\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000001\"}}";
        var json = WrapEvent("checkout.session.completed", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        var li = new StripeList<LineItem> { Data = new List<LineItem> { new LineItem { AmountTotal = 9900, Price = new Price { Nickname = "Pro" } } } };
        _lineItems.Setup(s => s.ListAsync("cs_test_1", It.IsAny<SessionLineItemListOptions>(), null, default)).ReturnsAsync(li);

        _stripeSub.Setup(s => s.GetAsync("sub_123", It.IsAny<SubscriptionGetOptions>(), null, default))
                  .ReturnsAsync(new Subscription
                  {
                      Id = "sub_123",
                      Status = "active",
                      BillingCycleAnchor = DateTimeOffset.FromUnixTimeSeconds(1735689600).UtcDateTime,
                      Items = new StripeList<SubscriptionItem>
                      {
                          Data = new List<SubscriptionItem>
                          {
                              new SubscriptionItem
                              {
                                  Price = new Price
                                  {
                                      Recurring = new PriceRecurring { Interval = "month", IntervalCount = 1 }
                                  }
                              }
                          }
                      }
                  });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => u.SubscriptionId == "sub_123" && u.IsActive),
                            "checkout_session:completed",
                            "cs_test_1",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
        _activity.Verify(a => a.WriteAsync(It.IsAny<ActivityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _stripeService.Verify(s => s.TryRefundAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutCompleted_NoMetadata_TriggersRefund()
    {
        var obj = "{\"id\":\"cs_test_2\",\"object\":\"checkout.session\",\"payment_status\":\"paid\",\"metadata\":{}}";
        var json = WrapEvent("checkout.session.completed", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _lineItems.Setup(s => s.ListAsync("cs_test_2", It.IsAny<SessionLineItemListOptions>(), null, default))
                  .ReturnsAsync(new StripeList<LineItem> { Data = new List<LineItem>() });

        await handler.HandleAsync(json, signature);

        _stripeService.Verify(s => s.TryRefundAsync(It.IsAny<string?>(), "missing_spred_user_id", "unknown"), Times.Once);
    }

    [Fact]
    public async Task CheckoutCompleted_InvalidGuid_TriggersRefund()
    {
        var obj = "{\"id\":\"cs_test_2b\",\"object\":\"checkout.session\",\"payment_status\":\"paid\",\"metadata\":{\"SpredUserId\":\"bad-guid\"}}";
        var json = WrapEvent("checkout.session.completed", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _lineItems.Setup(s => s.ListAsync("cs_test_2b", It.IsAny<SessionLineItemListOptions>(), null, default))
                  .ReturnsAsync(new StripeList<LineItem> { Data = new List<LineItem>() });

        await handler.HandleAsync(json, signature);

        _stripeService.Verify(s => s.TryRefundAsync(It.IsAny<string?>(), "invalid_spred_user_id", "bad-guid"), Times.Once);
    }

    [Fact]
    public async Task CheckoutCompleted_PaymentIntentResolvedViaSubscriptionInvoice()
    {
        var obj = "{\"id\":\"cs_test_1x\",\"object\":\"checkout.session\",\"payment_status\":\"paid\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000001\"},\"subscription\":{\"id\":\"sub_pi\"}}";
        var json = WrapEvent("checkout.session.completed", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _lineItems.Setup(s => s.ListAsync("cs_test_1x", It.IsAny<SessionLineItemListOptions>(), null, default))
                  .ReturnsAsync(new StripeList<LineItem> { Data = new List<LineItem>() });

        _stripeSub.Setup(s => s.GetAsync("sub_pi", It.IsAny<SubscriptionGetOptions>(), null, default))
                  .ReturnsAsync(new Subscription { Id = "sub_pi", Status = "active", BillingCycleAnchor = DateTime.UtcNow, LatestInvoiceId = "in_pi" });

        _invoiceService.Setup(s => s.GetAsync("in_pi", null, null, default))
                       .ReturnsAsync(new Invoice
                       {
                           Id = "in_pi",
                           Payments = new StripeList<InvoicePayment>
                           {
                               Data = new List<InvoicePayment>
                               {
                                   new InvoicePayment
                                   {
                                       Created = DateTime.UtcNow.AddMinutes(1),
                                       Payment = new InvoicePaymentPayment { PaymentIntentId = "pi_resolved" }
                                   }
                               }
                           }
                       });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => u.PaymentId == "pi_resolved"),
                            "checkout_session:completed",
                            "cs_test_1x",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task CheckoutAsyncPaymentSucceeded_BehavesLikeCompleted()
    {
        var obj = "{\"id\":\"cs_async_ok\",\"object\":\"checkout.session\",\"payment_status\":\"paid\",\"subscription\":{\"id\":\"sub_ok\"},\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000011\"}}";
        var json = WrapEvent("checkout.session.async_payment_succeeded", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _lineItems.Setup(s => s.ListAsync("cs_async_ok", It.IsAny<SessionLineItemListOptions>(), null, default))
                  .ReturnsAsync(new StripeList<LineItem> { Data = new List<LineItem>() });

        _stripeSub.Setup(s => s.GetAsync("sub_ok", It.IsAny<SubscriptionGetOptions>(), null, default))
                  .ReturnsAsync(new Subscription { Id = "sub_ok", Status = "active", BillingCycleAnchor = DateTime.UtcNow, Items = new StripeList<SubscriptionItem> { Data = new List<SubscriptionItem>() } });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => u.SubscriptionId == "sub_ok" && u.IsActive),
                            "checkout_session:completed",
                            "cs_async_ok",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task CheckoutAsyncPaymentFailed_SavesSnapshot()
    {
        var obj = "{\"id\":\"cs_async_fail\",\"object\":\"checkout.session\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000012\"}}";
        var json = WrapEvent("checkout.session.async_payment_failed", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.GetDetailsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((UserSubscriptionStatus?)null);

        _stateStore.Setup(s => s.SaveSnapshotAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            It.Is<string>(k => k.StartsWith("checkout_session:checkout.session.async_payment_failed")),
                            "cs_async_fail",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(Guid.NewGuid());

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task CheckoutExpired_SavesSnapshot()
    {
        var obj = "{\"id\":\"cs_expired\",\"object\":\"checkout.session\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000013\"}}";
        var json = WrapEvent("checkout.session.expired", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.GetDetailsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((UserSubscriptionStatus?)null);

        _stateStore.Setup(s => s.SaveSnapshotAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            It.Is<string>(k => k.StartsWith("checkout_session:checkout.session.expired")),
                            "cs_expired",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(Guid.NewGuid());

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task SubscriptionCreated_Active_SavesStatus()
    {
        var obj = "{\"id\":\"sub_created\",\"object\":\"subscription\",\"status\":\"active\",\"latest_invoice\":\"in_sc\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000021\"},\"items\":{\"data\":[{\"price\":{\"recurring\":{\"interval\":\"month\",\"interval_count\":1}}}]},\"billing_cycle_anchor\":1735689600}";
        var json = WrapEvent("customer.subscription.created", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _invoiceService.Setup(s => s.GetAsync("in_sc", null, null, default))
                       .ReturnsAsync(new Invoice
                       {
                           Id = "in_sc",
                           Payments = new StripeList<InvoicePayment>
                           {
                               Data = new List<InvoicePayment>
                               {
                                   new InvoicePayment{ Created = DateTime.UtcNow, Payment = new InvoicePaymentPayment{ PaymentIntentId = "pi_sc" } }
                               }
                           }
                       });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => u.SubscriptionId == "sub_created" && u.PaymentId == "pi_sc" && u.IsActive),
                            "subscription:created:active",
                            "sub_created",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
        _activity.Verify(a => a.WriteAsync(It.IsAny<ActivityRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscriptionUpdated_Canceled_WritesActivity()
    {
        var obj = "{\"id\":\"sub_abc\",\"object\":\"subscription\",\"status\":\"canceled\",\"latest_invoice\":\"in_1\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000002\"},\"items\":{\"data\":[{\"price\":{\"recurring\":{\"interval\":\"month\",\"interval_count\":1}}}]}}";
        var json = WrapEvent("customer.subscription.updated", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _invoiceService.Setup(s => s.GetAsync("in_1", null, null, default)).ReturnsAsync(new Invoice { Id = "in_1", Payments = new StripeList<InvoicePayment>() });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => !u.IsActive && u.LogicalState == "canceled"),
                            "subscription:updated:canceled",
                            "sub_abc",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _activity.Verify(a => a.WriteAsync(It.IsAny<ActivityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubscriptionDeleted_WritesActivity()
    {
        var obj = "{\"id\":\"sub_del\",\"object\":\"subscription\",\"status\":\"canceled\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000031\"}}";
        var json = WrapEvent("customer.subscription.deleted", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => !u.IsActive && u.SubscriptionId == "sub_del"),
                            "subscription:deleted",
                            "sub_del",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _activity.Verify(a => a.WriteAsync(It.IsAny<ActivityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvoicePaymentSucceeded_SavesStatus()
    {
        var obj = "{\"id\":\"in_2\",\"object\":\"invoice\",\"period_start\":1735689600,\"period_end\":1738368000,\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000003\",\"SubscriptionId\":\"sub_i2\"}}";
        var json = WrapEvent("invoice.payment_succeeded", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _invoiceService.Setup(s => s.GetAsync("in_2", null, null, default))
                       .ReturnsAsync(new Invoice
                       {
                           Id = "in_2",
                           Payments = new StripeList<InvoicePayment>
                           {
                               Data = new List<InvoicePayment>
                               {
                                   new InvoicePayment{ Created = DateTime.UtcNow, Payment = new InvoicePaymentPayment{ PaymentIntentId = "pi_i2" } }
                               }
                           }
                       });

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => u.SubscriptionId == "sub_i2" && u.PaymentId == "pi_i2" && u.IsActive),
                            "invoice:invoice.payment_succeeded",
                            "in_2",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Theory]
    [InlineData("invoice.finalized")]
    [InlineData("invoice.finalization_failed")]
    [InlineData("invoice.payment_failed")]
    public async Task Invoice_OtherEvents_SaveSnapshot(string evtType)
    {
        var obj = "{\"id\":\"in_x\",\"object\":\"invoice\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000041\"}}";
        var json = WrapEvent(evtType, obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.SaveSnapshotAsync(
                            It.IsAny<Guid>(),
                            Guid.Empty,
                            $"invoice:{evtType}",
                            "in_x",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(Guid.NewGuid());

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task PaymentIntent_Succeeded_SavesSnapshot()
    {
        var obj = "{\"id\":\"pi_ok\",\"object\":\"payment_intent\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000051\"}}";
        var json = WrapEvent("payment_intent.succeeded", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.GetDetailsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((UserSubscriptionStatus?)null);

        _stateStore.Setup(s => s.SaveSnapshotAsync(
                            It.IsAny<Guid>(),
                            Guid.Empty,
                            "payment_intent:payment_intent.succeeded",
                            "pi_ok",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(Guid.NewGuid());

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task RefundUpdated_SavesSnapshotOnly()
    {
        var obj = "{\"id\":\"re_1\",\"object\":\"refund\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000004\"}}";
        var json = WrapEvent("refund.updated", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.GetDetailsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new UserSubscriptionStatus { UserId = Guid.Parse("00000000-0000-0000-0000-000000000004") });

        _stateStore.Setup(s => s.SaveSnapshotAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            "refund:updated",
                            "re_1",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(Guid.NewGuid());

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyAll();
    }

    [Fact]
    public async Task ChargeRefunded_DeactivatesAndActivity()
    {
        var obj = "{\"id\":\"ch_1\",\"object\":\"charge\",\"payment_intent\":\"pi_1\",\"metadata\":{\"SpredUserId\":\"00000000-0000-0000-0000-000000000005\"}}";
        var json = WrapEvent("charge.refunded", obj);
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        _stateStore.Setup(s => s.SaveAtomicAsync(
                            It.IsAny<Guid>(),
                            It.Is<UserSubscriptionStatus>(u => !u.IsActive && u.LogicalState == "charge refunded" && u.PaymentId == "pi_1"),
                            "charge:refunded",
                            "ch_1",
                            It.IsAny<string>(),
                            default))
                   .ReturnsAsync(new AtomicSaveResult(true, true, null, null, HttpStatusCode.OK, null));

        await handler.HandleAsync(json, signature);

        _activity.Verify(a => a.WriteAsync(It.IsAny<ActivityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_InvalidSignature_ThrowsUnauthorized()
    {
        var json = WrapEvent("payment_intent.succeeded", "{\"id\":\"pi_x\",\"object\":\"payment_intent\"}");
        var signature = "t=1,v1=deadbeef";
        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(json, signature));
    }

    [Fact]
    public async Task UnhandledEventType_DoesNothing()
    {
        var json = WrapEvent("product.created", "{\"id\":\"pr_1\",\"object\":\"product\"}");
        var signature = Sign(_secret, json);
        var handler = CreateHandler();

        await handler.HandleAsync(json, signature);

        _stateStore.VerifyNoOtherCalls();
        _stripeService.VerifyNoOtherCalls();
        _activity.VerifyNoOtherCalls();
        _lineItems.VerifyNoOtherCalls();
        _stripeSub.VerifyNoOtherCalls();
        _invoiceService.VerifyNoOtherCalls();
    }
}
