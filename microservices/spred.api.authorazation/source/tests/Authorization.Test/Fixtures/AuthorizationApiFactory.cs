using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Test.Helpers;
using Extensions.Configuration;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace Authorization.Test.Fixtures;

/// <summary>
/// Auth api test factory
/// </summary>
public class AuthorizationApiFactory : WebApplicationFactory<Program>
{
    public Mock<IAnalyticsService> AnalyticsMock { get; } = new();
    public Mock<IGetToken> GetTokenMock { get; } = new();
    public Mock<IConnectionMultiplexer> ConnectionMultiplexerMock { get; } = new();
    public Mock<IThumbprintService> ThumbServiceMock { get; } = new();
    public Mock<IDatabase> RedisDbMock { get; } = new();
    public Mock<IUserPlusStore> UserStoreMock { get; } = new();
    public Mock<IPersistenceStore<BaseUser, Guid>> UsersMock { get; private set; }
    public Mock<IPersistenceStore<OAuthAuthentication, Guid>> OAuthMock { get; private set; }
    public Mock<IPersistenceStore<NotifyMe, Guid>> NotifyMock { get; private set; }
    public Mock<IPersistenceStore<Feedback, Guid>> FeedbackMock { get; private set; }
    public Mock<IPersistenceStore<BaseRole, Guid>> RolesMock { get; private set; }
    public Mock<IPersistenceStore<LinkedAccountEvent, Guid>> LinkedAccountEvents { get; private set; }

    public AuthorizationApiFactory()
    {
        UsersMock = new Mock<IPersistenceStore<BaseUser, Guid>>(MockBehavior.Strict);
        OAuthMock = new Mock<IPersistenceStore<OAuthAuthentication, Guid>>(MockBehavior.Strict);
        NotifyMock = new Mock<IPersistenceStore<NotifyMe, Guid>>(MockBehavior.Strict);
        FeedbackMock = new Mock<IPersistenceStore<Feedback, Guid>>(MockBehavior.Strict);
        RolesMock = new Mock<IPersistenceStore<BaseRole, Guid>>(MockBehavior.Strict);
        LinkedAccountEvents = new Mock<IPersistenceStore<LinkedAccountEvent, Guid>>(MockBehavior.Strict);
        

        SetupPersistenceStoreMock<BaseUser, Guid, object>(UsersMock, SeedBaseUser);
        SetupPersistenceStoreMock<OAuthAuthentication, Guid, object>(OAuthMock, SeedOAuth);
        SetupPersistenceStoreMock<NotifyMe, Guid, object>(NotifyMock, SeedNotify);
        SetupPersistenceStoreMock<Feedback, Guid, object>(FeedbackMock, SeedFeedback);
        SetupPersistenceStoreMock<BaseRole, Guid, object>(RolesMock, SeedRole);
        SetupPersistenceStoreMock<LinkedAccountEvent, Guid, long>(LinkedAccountEvents, () => new LinkedAccountEvent()
        {
            AccountId = "test",
            CorrelationId = Guid.NewGuid(),
            EventType = LinkedAccountEventType.AccountCreated,
            Payload = null,
            Platform = AccountPlatform.Spotify,
            Sequence = 1,
            UserId = Guid.NewGuid()
        });;
    }

    private void SetUpRedisDatabase()
    {
        RedisDbMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true)
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((key, value, _, _, _) =>
            {
                _store[key] = value;
            });

        RedisDbMock.Setup(db => db.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                _store.TryGetValue(key, out var value);
                return value;
            });

        RedisDbMock.Setup(db => db.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                return _store.Remove(key);
            });


        RedisDbMock.Setup(db => db.HashSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<HashEntry[]>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true))
            .Callback<RedisKey, HashEntry[], CommandFlags>((key, entries, _) =>
            {
                var keyStr = key.ToString();
                if (!_hashStore.ContainsKey(keyStr))
                    _hashStore[keyStr] = new Dictionary<string, string>();

                foreach (var entry in entries)
                {
                    _hashStore[keyStr][entry.Name!] = entry.Value!;
                }
            });

        RedisDbMock.Setup(db => db.HashGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();

                if (keyStr.Contains("signing"))
                    return _signingInternalKey;

                if (keyStr.Contains("decrypt"))
                    return _decryptionKey;

                return _hashStore.TryGetValue(keyStr, out var fields) && fields.TryGetValue(fieldStr, out var val)
                    ? (RedisValue)val
                    : RedisValue.Null;
            });

        RedisDbMock.Setup(db => db.HashExistsAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();

                var exists = _hashStore.TryGetValue(keyStr, out var fields) && fields.ContainsKey(fieldStr);
                return Task.FromResult(exists);
            });
    }

    private readonly string _signingInternalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _decryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<RedisKey, RedisValue> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _hashStore = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        SetUpRedisDatabase();
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStoreMock.Object, Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
                new Mock<ILookupNormalizer>().Object,
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole<Guid>>>>().Object
            );
            
            services.RemoveAll<RoleManager<IdentityRole<Guid>>>();
            services.AddSingleton(roleManagerMock.Object);
            
            services.RemoveAll<IAnalyticsService>();
            services.AddSingleton(AnalyticsMock.Object);
    
            services.RemoveAll<IPersistenceStore<BaseUser, Guid>>();
            services.AddScoped(u => UsersMock.Object);
            services.RemoveAll<IPersistenceStore<OAuthAuthentication, Guid>>();
            services.AddScoped(u => OAuthMock.Object);
            services.RemoveAll<IPersistenceStore<NotifyMe, Guid>>();
            services.AddScoped(u => NotifyMock.Object);
            services.RemoveAll<IPersistenceStore<Feedback, Guid>>();
            services.AddScoped(u => FeedbackMock.Object);
            services.RemoveAll<IPersistenceStore<BaseRole, Guid>>();
            services.AddScoped(u => RolesMock.Object);
            services.RemoveAll<IPersistenceStore<LinkedAccountEvent, Guid>>();
            services.AddScoped(u => LinkedAccountEvents.Object);

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

            services.RemoveAll<IUserPlusStore>();
            services.AddScoped(_ =>
            {
                UserStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BaseUser()
                   {
                       UserName = "test",
                       Id = Guid.NewGuid(),
                       Email = "test@empty.com",
                       SecurityStamp = "emptySid"
                   });
                UserStoreMock.Setup(x => x.FindUserByPrimaryIdAsync(It.IsAny<string>(), It.IsAny<AuthType>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new BaseUser()
                   {
                       UserName = "test",
                       Id = Guid.Empty,
                       Email = "@empty",
                       SecurityStamp = "emptySid"
                   });                
                UserStoreMock.Setup(x => x.GetUserOAuthAuthentication(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid userId, CancellationToken _) =>
                    {
                        var list = new List<OAuthAuthentication>
                        {
                            new()
                            {
                                SpredUserId = userId, // вот он, userId из аргумента
                                PrimaryId = "spotify-client-id-123",
                                OAuthProvider = AuthType.Spotify.ToString()
                            }
                        };

                        return Task.FromResult(list);
                    });
                UserStoreMock.As<IUserAuthenticationTokenStore<BaseUser>>()
                    .Setup(x => x.GetTokenAsync(
                        It.IsAny<BaseUser>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync("mock-refresh-token");

                UserStoreMock.Setup(x => x.UpdateAsync(It.IsAny<BaseUser>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(IdentityResult.Success);
                
                return UserStoreMock.Object;
            });

            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("TestScheme")
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy(JwtSpredPolicy.JwtServicePolicy, policy =>
                    policy.RequireAssertion(_ => true));
                options.AddPolicy(JwtSpredPolicy.JwtUserPolicy, policy =>
                    policy.RequireAssertion(_ => true));
                options.AddPolicy("COOKIE_OR_OAUTH", policy =>
                {
                    policy.AddAuthenticationSchemes("TestScheme");
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme, policy =>
                {
                    policy.AddAuthenticationSchemes("TestScheme");
                    policy.RequireAuthenticatedUser();
                });
            });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            });
        });
    }
    
    public static void SetupPersistenceStoreMock<T, TKey, TSort>(
    Mock<IPersistenceStore<T, TKey>> mock,
    Func<T> seedData
    ) where T : class, IBaseEntity<TKey>
    {
        mock.Setup(x => x.StoreAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<T, CancellationToken>((entity, _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var existing = mockData.FirstOrDefault(e => Equals(e.Id, entity.Id));
                if (existing != null)
                    mockData.Remove(existing);
                mockData.Add(entity);
            });

        mock.Setup(x => x.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<T, CancellationToken>((entity, _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var existing = mockData.FirstOrDefault(e => Equals(e.Id, entity.Id));
                if (existing != null)
                {
                    mockData.Remove(existing);
                    mockData.Add(entity);
                }
            });

        mock.Setup(x => x.DeleteAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<T, CancellationToken>((entity, _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                mockData.RemoveAll(e => Equals(e.Id, entity.Id));
            });

        mock.Setup(x => x.DeleteAsync(It.IsAny<TKey>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<TKey, PartitionKey, CancellationToken>((id, _, _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                mockData.RemoveAll(e => Equals(e.Id, id));
            });

        mock.Setup(x => x.GetAsync(It.IsAny<TKey>(), It.IsAny<PartitionKey>(), 
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((TKey id, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var entity = mockData.FirstOrDefault(x => Equals(x.Id, id));
                return new PersistenceResult<T>(entity!, false, null);
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

        mock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var compiled = filter?.Compile();
                var count = compiled != null ? mockData.Count(compiled) : mockData.Count;
                return new PersistenceResult<int>(count, false, null);
            });
        
        mock.Setup(x => x.GetAllAsync(It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>()))
            .Returns((PartitionKey _, CancellationToken ct) => ToAsyncEnumerable<T>(seedData, ct));
    }
    
    static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(Func<T> source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var obj = source();
        ct.ThrowIfCancellationRequested();
        yield return obj;
        await Task.Yield();
    }
    
    private static BaseUser SeedBaseUser()
    {
        return new BaseUser
        {
            Id = Guid.NewGuid(),
            UserName = "seed",
            NormalizedUserName = "SEED",
            Email = "seed@example.com",
            NormalizedEmail = "SEED@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            UserRoles = ["ARTIST"]
        };
    }

    private static OAuthAuthentication SeedOAuth()
    {
        return new OAuthAuthentication
        {
            PrimaryId = "test",
            OAuthProvider = AuthType.Base.ToString(),
            SpredUserId = Guid.Empty
        };
    }

    private static NotifyMe SeedNotify()
    {
        return new NotifyMe
        { };
    }

    private static Feedback SeedFeedback()
    {
        return new Feedback
        { };
    }

    private static BaseRole SeedRole()
    {
        return new BaseRole
        {
            Name = "seed-role",
            NormalizedName = "SEED-ROLE"
        };
    }

    public void VerifyAll()
    {
        UsersMock.VerifyAll();
        OAuthMock.VerifyAll();
        NotifyMock.VerifyAll();
        FeedbackMock.VerifyAll();
        RolesMock.VerifyAll();
    }
}