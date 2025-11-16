using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Components;
using SubscriptionService.Configurations;
using SubscriptionService.Models;

namespace SubscriptionService.Test;

public class StripeServiceTests
{
    private readonly Mock<SessionService> _sessionServiceMock;
    private readonly Mock<Stripe.SubscriptionService> _subscriptionServiceMock;
    private readonly Mock<RefundService> _refundServiceMock;
    private readonly StripeService _service;
    private readonly StripeOptions _stripeOptions;

    public StripeServiceTests()
    {
        _sessionServiceMock = new Mock<SessionService>();
        _subscriptionServiceMock = new Mock<Stripe.SubscriptionService>();
        _refundServiceMock = new Mock<RefundService>();

        _stripeOptions = new StripeOptions
        {
            SecretKey = "sk_test_123",
            PublicKey = "pk_test_123",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            Plans = new Dictionary<string, string> { { "premium", "price_123" } }
        };

        _service = new StripeService(
            Options.Create(_stripeOptions),
            _sessionServiceMock.Object,
            _subscriptionServiceMock.Object,
            _refundServiceMock.Object,
            NullLoggerFactory.Instance
        );
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ShouldReturnSessionId_WhenPlanIsValid()
    {
        var session = new Session { Id = "sess_123" };
        _sessionServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _service.CreateCheckoutSessionAsync(
            new CheckoutRequest("premium"),
            "user@example.com",
            "user_001"
        );

        Assert.Equal("sess_123", result);
        _sessionServiceMock.Verify(x => x.CreateAsync(It.Is<SessionCreateOptions>(o =>
            o.Mode == "subscription" &&
            o.CustomerEmail == "user@example.com" &&
            o.Metadata["SpredUserId"] == "user_001"
        ), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ShouldThrow_WhenPlanIsInvalid()
    {
        var request = new CheckoutRequest("invalid_plan");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateCheckoutSessionAsync(request, "user@example.com", "user_001"));
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ShouldInvokeStripeService()
    {
        _subscriptionServiceMock
            .Setup(x => x.CancelAsync("sub_123", null, It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription());

        await _service.CancelSubscriptionAsync("sub_123");

        _subscriptionServiceMock.Verify(x =>
            x.CancelAsync("sub_123", null, It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryRefundAsync_ShouldNotCallStripe_WhenPaymentIntentIsMissing()
    {
        await _service.TryRefundAsync(null, "Test reason", "user_001");

        _refundServiceMock.Verify(
            x => x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryRefundAsync_ShouldCreateRefund_WhenValid()
    {
        var refund = new Refund { Id = "re_123" };
        _refundServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        await _service.TryRefundAsync("pi_123", "User requested", "user_001");

        _refundServiceMock.Verify(x => x.CreateAsync(It.Is<RefundCreateOptions>(o =>
            o.PaymentIntent == "pi_123" &&
            o.Metadata["SpredUserId"] == "user_001" &&
            o.Reason == RefundReasons.RequestedByCustomer
        ), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryRefundAsync_ShouldCatchStripeException()
    {
        _refundServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("Refund failed"));

        await _service.TryRefundAsync("pi_999", "Test", "user_001");

        // Проверка: метод должен обработать исключение, не пробрасывая его наружу
        _refundServiceMock.Verify(x =>
            x.CreateAsync(It.IsAny<RefundCreateOptions>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
