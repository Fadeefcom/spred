using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Moq;
using Stripe;
using SubscriptionService.Models;
using SubscriptionService.Models.Entities;
using SubscriptionService.Test.Fixtures;

namespace SubscriptionService.Test;

public class SubscriptionRoutesTests : IClassFixture<SubscriptionApiFactory>
{
    private readonly SubscriptionApiFactory _factory;

    public SubscriptionRoutesTests(SubscriptionApiFactory factory)
    {
        _factory = factory;
        _factory.EnableTestAuth = true;
    }

    private HttpClient CreateClientWithUser(Guid userId, bool isActive = true)
    {
        _factory.SubscriptionStateStore = SubscriptionApiFactory.CreateDefault(userId, isActive);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "fake-jwt");
        return client;
    }

    [Fact]
    public async Task Checkout_Should_Return_SessionId()
    {
        var client = CreateClientWithUser(Guid.NewGuid(), isActive: false);

        var request = new CheckoutRequest("premium-monthly");
        var response = await client.PostAsJsonAsync("/subscription/checkout", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("sess_test", result!["sessionId"]);

        _factory.SessionServiceMock.Verify(x => x.CreateAsync(
                It.IsAny<Stripe.Checkout.SessionCreateOptions>(),
                It.IsAny<Stripe.RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _factory.SubscriptionStateStore.Verify(s =>
            s.GetStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_Should_Return_Ok_And_Use_StripeSubService_And_Update_Status()
    {
        var userId = Guid.Empty;
        var client = CreateClientWithUser(userId, isActive: true);

        _factory.SubscriptionStateStore
            .Setup(s => s.SetStatusAsync(
                userId,
                It.IsAny<string>(),
                false,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/subscription/cancel", new CancelSubscriptionRequest("test-reason"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _factory.StripeSubServiceMock.Verify(x => x.CancelAsync(
                It.IsAny<string>(),
                It.IsAny<Stripe.SubscriptionCancelOptions>(),
                It.IsAny<Stripe.RequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _factory.SubscriptionStateStore.Verify(s => s.SetStatusAsync(
            userId,
            It.IsAny<string>(),
            false,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Status_Should_Return_Json_With_Active_Field_And_Call_Store()
    {
        var userId = Guid.Empty;
        var client = CreateClientWithUser(userId, isActive: true);

        _factory.SubscriptionStateStore
            .Setup(s => s.GetDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSubscriptionStatus
            {
                UserId = userId,
                IsActive = true,
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-1),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(29),
                LogicalState = "active"
            });

        var response = await client.GetAsync("/subscription/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(doc);
        var root = doc!.RootElement;

        Assert.True(root.TryGetProperty("isActive", out var isActiveProp));
        Assert.True(isActiveProp.GetBoolean());

        Assert.True(root.TryGetProperty("currentPeriodStart", out _));
        Assert.True(root.TryGetProperty("currentPeriodEnd", out _));
        Assert.True(root.TryGetProperty("logicalState", out var logicalStateProp));
        Assert.Equal("active", logicalStateProp.GetString());

        _factory.SubscriptionStateStore.Verify(
            s => s.GetDetailsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Status_Should_Return_Inactive_When_Status_Not_Found()
    {
        var userId = Guid.Empty;
        var client = CreateClientWithUser(userId, isActive: false);

        _factory.SubscriptionStateStore
            .Setup(s => s.GetDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSubscriptionStatus?)null);

        var response = await client.GetAsync("/subscription/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(doc);
        var root = doc!.RootElement;

        Assert.True(root.TryGetProperty("isActive", out var isActiveProp));
        Assert.False(isActiveProp.GetBoolean());

        _factory.SubscriptionStateStore.Verify(
            s => s.GetDetailsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_Should_Throw_When_No_Signature()
    {
        var client = CreateClientWithUser(Guid.NewGuid());

        var content = new StringContent("{}");
        var response = await client.PostAsync("/internal/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_Should_Return_Ok_When_Signature_Present()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var client = CreateClientWithUser(userId);

        var secret = "whsec_XXXXXXXXXXXXXXXXXXXXXXXX";
        var json = """
                   {
                     "id": "evt_test_123",
                     "object": "event",
                     "api_version": "2025-09-30.clover",
                     "created": 1739583894,
                     "livemode": false,
                     "pending_webhooks": 1,
                     "request": { "id": "req_123", "idempotency_key": null },
                     "type": "checkout.session.completed",
                     "data": {
                       "object": {
                         "id": "cs_test_123",
                         "object": "checkout.session",
                         "payment_status": "paid",
                         "metadata": { "SpredUserId": "00000000-0000-0000-0000-000000000001" }
                       },
                       "previous_attributes": {}
                     }
                   }
                   """;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
        var stripeHeader = $"t={timestamp},v1={signature}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/stripe/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", stripeHeader);

        _factory.SubscriptionStateStore
            .Setup(s => s.SaveAtomicAsync(
                userId,
                It.IsAny<UserSubscriptionStatus>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtomicSaveResult(true, true, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), HttpStatusCode.OK, string.Empty));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _factory.SubscriptionStateStore.Verify(s => s.SaveAtomicAsync(
            userId,
            It.IsAny<UserSubscriptionStatus>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Webhook_Should_LogWarning_When_MetadataMissing()
    {
        var client = CreateClientWithUser(Guid.NewGuid());

        var secret = "whsec_XXXXXXXXXXXXXXXXXXXXXXXX";
        var json = """
                   {
                     "id": "evt_test_124",
                     "object": "event",
                     "api_version": "2025-09-30.clover",
                     "created": 1739583894,
                     "livemode": false,
                     "pending_webhooks": 1,
                     "request": { "id": "req_124", "idempotency_key": null },
                     "type": "checkout.session.completed",
                     "data": {
                       "object": {
                         "id": "cs_test_124",
                         "object": "checkout.session",
                         "payment_status": "paid",
                         "metadata": {}
                       },
                       "previous_attributes": {}
                     }
                   }
                   """;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
        var stripeHeader = $"t={timestamp},v1={signature}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/stripe/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", stripeHeader);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _factory.SubscriptionStateStore.Verify(s =>
            s.SaveAtomicAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserSubscriptionStatus>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Webhook_Should_Process_Subscription_With_Recurring_Interval_And_Persist()
    {
        var userId = Guid.Empty;
        var client = CreateClientWithUser(userId);

        var secret = "whsec_XXXXXXXXXXXXXXXXXXXXXXXX";
        var json = """
                   {
                     "id": "evt_test_125",
                     "object": "event",
                     "api_version": "2025-09-30.clover",
                     "created": 1739583894,
                     "livemode": false,
                     "pending_webhooks": 1,
                     "request": { "id": "req_125", "idempotency_key": null },
                     "type": "checkout.session.completed",
                     "data": {
                       "object": {
                         "id": "cs_test_125",
                         "object": "checkout.session",
                         "payment_status": "paid",
                         "subscription": { "id": "sub_test_001" },
                         "metadata": { "SpredUserId": "00000000-0000-0000-0000-000000000000" }
                       },
                       "previous_attributes": {}
                     }
                   }
                   """;

        var fakeSubscription = new Stripe.Subscription
        {
            Id = "sub_test_200",
            BillingCycleAnchor = DateTime.UtcNow,
            Items = new StripeList<SubscriptionItem>
            {
                Data = new List<SubscriptionItem>
                {
                    new()
                    {
                        Price = new Price
                        {
                            Recurring = new PriceRecurring
                            {
                                Interval = "month",
                                IntervalCount = 2
                            }
                        }
                    }
                }
            }
        };

        _factory.StripeSubServiceMock
            .Setup(x => x.GetAsync("sub_test_001", It.IsAny<SubscriptionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeSubscription);

        _factory.SubscriptionStateStore
            .Setup(s => s.SaveAtomicAsync(
                userId,
                It.Is<UserSubscriptionStatus>(us => us.IsActive == true),
                "checkout.session.completed",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtomicSaveResult(true, true, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), HttpStatusCode.OK, string.Empty));

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
        var stripeHeader = $"t={timestamp},v1={signature}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/stripe/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", stripeHeader);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _factory.StripeSubServiceMock.Verify(x =>
                x.GetAsync("sub_test_001", It.IsAny<SubscriptionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _factory.SubscriptionStateStore.Verify(s => s.SaveAtomicAsync(
            userId,
            It.IsAny<UserSubscriptionStatus>(),
            "checkout_session:completed",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
