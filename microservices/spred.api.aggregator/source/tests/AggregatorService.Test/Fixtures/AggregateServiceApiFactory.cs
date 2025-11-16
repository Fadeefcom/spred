using System.Linq.Expressions;
using System.Security.Cryptography;
using AggregatorService.Abstractions;
using AggregatorService.Test.Helpers;
using Extensions.Configuration;
using Extensions.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using StackExchange.Redis;

namespace AggregatorService.Test.Fixtures;

public class AggregateServiceApiFactory : WebApplicationFactory<Program>
{
    public Mock<IGetToken> GetTokenMock { get; } = new();
    public Mock<IConnectionMultiplexer> ConnectionMultiplexerMock { get; } = new();
    public Mock<IThumbprintService> ThumbServiceMock { get; } = new();
    public Mock<IDatabase> RedisDbMock { get; } = new();
    public Mock<ITrackDownloadService> TrackDownloadServiceMock { get; } = new();
    public Mock<ITrackSenderService> TrackSenderServiceMock { get; } = new();
    
    public Mock<IParserAccessGate> ParserAccessGateMock { get; } = new();
    
    public Mock<ISendEndpointProvider> BusSendEndpointMock { get; } = new();
    public Mock<ISendEndpoint> SendEndpointMock { get; } = new();
    
    public bool EnableTestAuth { get; set; } = true;
    
    private readonly string _signingInternalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _decryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<RedisKey, RedisValue> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _hashStore = new();
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        SetUpRedisDatabase();
        
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
            });

            services.RemoveAll<IParserAccessGate>();
            services.AddSingleton(ParserAccessGateMock.Object);
            
            services.RemoveAll<IGetToken>();
            services.AddSingleton(GetTokenMock.Object);
            
            // Add mock services for routes
            services.RemoveAll<ITrackDownloadService>();
            services.AddSingleton(TrackDownloadServiceMock.Object);
            services.AddSingleton(TrackDownloadServiceMock);
            
            services.RemoveAll<ITrackSenderService>();
            services.AddSingleton(TrackSenderServiceMock.Object);
            services.AddSingleton(TrackSenderServiceMock);
            
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
            }
            
            services.AddSingleton<ISendEndpointProvider>(BusSendEndpointMock.Object);

            // Возвращаем настроенный endpoint
            BusSendEndpointMock
                .Setup(p => p.GetSendEndpoint(It.IsAny<Uri>()))
                .ReturnsAsync(SendEndpointMock.Object);

            // Stub чтобы Send не зависал
            SendEndpointMock
                .Setup(p => p.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        });
        

        return base.CreateHost(builder);
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

    public void SetupPersistenceStoreMock<T, TKey, TSort>(
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

        mock.Setup(x => x.DeleteAsync(It.IsAny<TKey>(), 
                It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<TKey, PartitionKey, CancellationToken>((id, _, _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                mockData.RemoveAll(e => Equals(e.Id, id));
            });

        mock.Setup(x => x.GetAsync(It.IsAny<TKey>(), 
                It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
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
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
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
    }
}