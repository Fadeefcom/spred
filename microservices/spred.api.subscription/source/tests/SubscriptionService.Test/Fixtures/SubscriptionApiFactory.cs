using System.Linq.Expressions;
using System.Net;
using System.Security.Cryptography;
using Extensions.Configuration;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using Spred.Bus.Abstractions;
using StackExchange.Redis;
using Stripe;
using Stripe.Checkout;
using SubscriptionService.Abstractions;
using SubscriptionService.Models;
using SubscriptionService.Models.Entities;
using SubscriptionService.Test.Helpers;
using RequestOptions = Microsoft.Azure.Cosmos.RequestOptions;

namespace SubscriptionService.Test.Fixtures;

public class SubscriptionApiFactory : WebApplicationFactory<Program>
{
    public Mock<IGetToken> GetTokenMock { get; } = new();
    public Mock<IConnectionMultiplexer> ConnectionMultiplexerMock { get; } = new();
    public Mock<IThumbprintService> ThumbServiceMock { get; } = new();
    public Mock<IDatabase> RedisDbMock { get; } = new();
    public Mock<IPersistenceStore<UserSubscriptionStatus, Guid>> SubscriptionMock { get; } = new();
    
    public Mock<IPersistenceStore<SubscriptionSnapshot, Guid>> SubscriptionSnapshot { get; } = new();

    public Mock<ISubscriptionStateStore> SubscriptionStateStore = CreateDefault(Guid.Empty, true);
    
    public Mock<SessionService> SessionServiceMock { get; } = new();
    
    public Mock<Stripe.SubscriptionService> StripeSubServiceMock { get; } = new();
    
    public Mock<IActivityWriter> ActivityWriterMock { get; } = new();

    public Mock<SessionLineItemService> sessionLineItemServiceMock { get; } = new();
    
    public bool EnableTestAuth { get; set; } = true;

    private readonly string _signingInternalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _decryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<RedisKey, RedisValue> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _hashStore = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        SetupPersistenceStoreMock<UserSubscriptionStatus, Guid, long>(SubscriptionMock, () => new UserSubscriptionStatus
        {
            UserId = Guid.NewGuid(),
            IsActive = false
        });
        
        SetupPersistenceStoreMock<SubscriptionSnapshot, Guid, long>(SubscriptionSnapshot, () => new SubscriptionSnapshot
        {
            UserId = Guid.NewGuid(),
        });

        SetUpRedisDatabase();

        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Stripe mocks
            SessionServiceMock.Setup(x => x.CreateAsync(It.IsAny<SessionCreateOptions>(),
                    It.IsAny<Stripe.RequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Stripe.Checkout.Session { Id = "sess_test" });

            StripeSubServiceMock.Setup(x => x.CancelAsync(It.IsAny<string>(),
                    It.IsAny<SubscriptionCancelOptions>(), It.IsAny<Stripe.RequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Stripe.Subscription { Id = "sub_test", Status = "canceled" });
            
            sessionLineItemServiceMock
                .Setup(x => x.ListAsync(
                    It.IsAny<string>(),
                    It.IsAny<SessionLineItemListOptions?>(),
                    It.IsAny<Stripe.RequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StripeList<LineItem>
                {
                    Data = new List<LineItem>
                    {
                        new LineItem
                        {
                            Description = "Pro Plan",
                            AmountTotal = 1000,
                            Price = new Price
                            {
                                Nickname = "Pro Plan",
                                Recurring = new PriceRecurring
                                {
                                    Interval = "month",
                                    IntervalCount = 1
                                }
                            }
                        }
                    }
                });

            services.RemoveAll<SessionService>();
            services.RemoveAll<Stripe.SubscriptionService>();
            services.RemoveAll<SessionLineItemService>();

            services.RemoveAll<IActivityWriter>();
            services.AddScoped<IActivityWriter>(_ => ActivityWriterMock.Object);

            services.AddSingleton(SessionServiceMock.Object);
            services.AddSingleton(StripeSubServiceMock.Object);
            services.AddSingleton(sessionLineItemServiceMock.Object);
            
            services.RemoveAll<IGetToken>();
            services.AddSingleton(GetTokenMock.Object);

            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(_ =>
            {
                ConnectionMultiplexerMock
                    .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                    .Returns(RedisDbMock.Object);
                return ConnectionMultiplexerMock.Object;
            });

            services.RemoveAll<IThumbprintService>();
            services.AddSingleton(_ =>
            {
                ThumbServiceMock.Setup(x => x.Generate(It.IsAny<HttpContext>()))
                    .Returns("thumb");
                ThumbServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), It.IsAny<string>()))
                    .Returns(true);
                return ThumbServiceMock.Object;
            });

            if (EnableTestAuth)
            {
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, AuthHandlerHelper>("TestScheme", _ => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("TestScheme")
                        .RequireAuthenticatedUser()
                        .Build();

                    options.AddPolicy(JwtSpredPolicy.JwtServicePolicy, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(JwtSpredPolicy.JwtUserPolicy, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistCreate, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistDeleteOwn, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistEditOwn, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistOwnPrivateRead, p => p.RequireAssertion(_ => true));

                    options.AddPolicy("COOKIE_OR_OAUTH", p =>
                    {
                        p.AddAuthenticationSchemes("TestScheme");
                        p.RequireAuthenticatedUser();
                    });

                    options.AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme, p =>
                    {
                        p.AddAuthenticationSchemes("TestScheme");
                        p.RequireAuthenticatedUser();
                    });
                });
            }

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            });

            services.RemoveAll<IPersistenceStore<UserSubscriptionStatus, Guid>>();
            services.AddScoped(_ => SubscriptionMock.Object);
            
            services.RemoveAll<IPersistenceStore<SubscriptionSnapshot, Guid>>();
            services.AddScoped(_ => SubscriptionSnapshot.Object);

            services.RemoveAll<ISubscriptionStateStore>();
            services.AddScoped(_ => SubscriptionStateStore.Object);
        });

        return base.CreateHost(builder);
    }

    private void SetUpRedisDatabase()
    {
        RedisDbMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true)
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((key, value, _, _, _) =>
            {
                _store[key] = value;
            });

        RedisDbMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                _store.TryGetValue(key, out var value);
                return value;
            });

        RedisDbMock.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) => _store.Remove(key));

        RedisDbMock.Setup(db => db.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true))
            .Callback<RedisKey, HashEntry[], CommandFlags>((key, entries, _) =>
            {
                var keyStr = key.ToString();
                if (!_hashStore.ContainsKey(keyStr))
                    _hashStore[keyStr] = new();

                foreach (var entry in entries)
                    _hashStore[keyStr][entry.Name!] = entry.Value!;
            });

        RedisDbMock.Setup(db => db.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();

                if (keyStr.Contains("signing"))
                    return _signingInternalKey;
                if (keyStr.Contains("decrypt"))
                    return _decryptionKey;

                return _hashStore.TryGetValue(keyStr, out var fields) &&
                       fields.TryGetValue(fieldStr, out var val)
                    ? (RedisValue)val
                    : RedisValue.Null;
            });

        RedisDbMock.Setup(db => db.HashExistsAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();
                var exists = _hashStore.TryGetValue(keyStr, out var fields) && fields.ContainsKey(fieldStr);
                return Task.FromResult(exists);
            });
    }

    public static void SetupPersistenceStoreMock<T, TKey, TSort>(
        Mock<IPersistenceStore<T, TKey>> mock,
        Func<T> seedData
    ) where T : class, IBaseEntity<TKey>
    {
        mock.Setup(x => x.StoreAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.DeleteAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.GetAsync(It.IsAny<TKey>(), It.IsAny<PartitionKey>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((TKey id, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                var result = Equals(((IBaseEntity<TKey>)obj).Id, id) ? obj : null;
                return new PersistenceResult<T>(result!, false, null);
            });

        mock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                var data = new List<T> { obj };
                var compiled = filter?.Compile();
                var count = compiled != null ? data.Count(compiled) : data.Count;
                return new PersistenceResult<int>(count, false, null);
            });
        
        mock.Setup(x => x.GetAsync<TSort>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, TSort>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), 
                It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter,
                Expression<Func<T, TSort>> _,
                PartitionKey _,
                int _,
                int _,
                bool _,
                CancellationToken _,
                bool _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var compiled = filter?.Compile();
                var result = compiled != null ? mockData.Where(compiled).ToList() : mockData;
                return new PersistenceResult<IEnumerable<T>>(result, false, null);
            });
    }
    
    public static Mock<ISubscriptionStateStore> CreateDefault(Guid? userId = null, bool isActive = true, string subscriptionId = "sub_123")
    {
        var uid = userId ?? Guid.NewGuid();
        var status = new UserSubscriptionStatus
        {
            UserId = uid,
            IsActive = isActive,
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-10),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(20),
            SubscriptionId = subscriptionId,
        };

        var mock = new Mock<ISubscriptionStateStore>(MockBehavior.Strict);

        mock.Setup(s => s.GetStatusAsync(uid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isActive);

        mock.Setup(s => s.GetDetailsAsync(uid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSubscriptionStatus
            {
                UserId = status.UserId,
                IsActive = isActive,
                CurrentPeriodStart = status.CurrentPeriodStart,
                CurrentPeriodEnd = status.CurrentPeriodEnd,
                SubscriptionId = subscriptionId,
            });

        mock.Setup(s => s.SetStatusAsync(
                uid,
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        mock.Setup(s => s.SaveSnapshotAsync(
                uid,
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        mock.Setup(s => s.SaveAtomicAsync(
                uid,
                It.IsAny<UserSubscriptionStatus>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AtomicSaveResult(true, true, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), HttpStatusCode.OK, string.Empty));

        return mock;
    }
}
