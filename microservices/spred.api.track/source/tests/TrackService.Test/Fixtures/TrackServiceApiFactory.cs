using System.Linq.Expressions;
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
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using StackExchange.Redis;
using TrackService.Abstractions;
using TrackService.Models.Entities;
using TrackService.Test.Helpers;
using Xabe.FFmpeg;

namespace TrackService.Test.Fixtures;

public class TrackServiceApiFactory : WebApplicationFactory<Program>
{
    public Mock<IGetToken> GetTokenMock { get; } = new();
    public Mock<IConnectionMultiplexer> ConnectionMultiplexerMock { get; } = new();
    public Mock<IThumbprintService> ThumbServiceMock { get; } = new();
    public Mock<IDatabase> RedisDbMock { get; } = new();
    public Mock<IPersistenceStore<TrackMetadata, Guid>> CatalogDataMock { get; } = new();
    public Mock<IPersistenceStore<TrackPlatformId, Guid>> PlatformIdMock { get; } = new();
    public Mock<IMediaInfo> MockMediaInfo { get; } = new();
    public Mock<IFFmpegWrapper> FfmpegMock { get; } = new();

    public Mock<IAudioStream> MockAudioStream { get; } = new();
    
    public bool EnableTestAuth { get; set; } = true;
    
    private readonly string _signingInternalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _decryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<RedisKey, RedisValue> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _hashStore = new();
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        SetupPersistenceStoreMock<TrackMetadata, Guid, long>(CatalogDataMock, TestObjectsHelper.InitTestTrackMetadata);
        SetupPersistenceStoreMock<TrackPlatformId, Guid, long>(PlatformIdMock, () => new TrackPlatformId()
        {
            SpredUserId = Guid.Empty,
            Platform = Platform.Spotify,
            PlatformTrackId = "test",
            TrackMetadataId = Guid.NewGuid()
        });
        SetUpRedisDatabase();
        
        MockAudioStream.Setup(a => a.Bitrate).Returns(128000);
        MockAudioStream.Setup(a => a.Channels).Returns(2);
        MockAudioStream.Setup(a => a.Duration).Returns(TimeSpan.FromSeconds(180));
        MockAudioStream.Setup(a => a.SampleRate).Returns(44100);
        MockAudioStream.Setup(a => a.Codec).Returns("aac");

        MockMediaInfo.Setup(m => m.AudioStreams).Returns(new List<IAudioStream> { MockAudioStream.Object });
        FfmpegMock.Setup(f => f.GetMediaInfo(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockMediaInfo.Object);
        
        builder.UseEnvironment("Test");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(); // Вывод в консоль
            logging.AddDebug();   // Для IDE Output (Visual Studio / Rider)
            logging.SetMinimumLevel(LogLevel.Information);
        });
        builder.ConfigureServices(services =>
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConsole();
            });
            
            services.RemoveAll<IPersistenceStore<TrackMetadata, Guid>>();
            services.AddScoped(_ => CatalogDataMock.Object);

            services.RemoveAll<IPersistenceStore<TrackPlatformId, Guid>>();
            services.AddScoped(_ => PlatformIdMock.Object);
            
            services.RemoveAll<IFFmpegWrapper>();
            services.AddSingleton(_ => FfmpegMock.Object);
            
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
                    options.AddPolicy(JwtSpredPolicy.JwtServicePolicy, policy =>
                        policy.RequireAssertion(_ => true));
                    options.AddPolicy(JwtSpredPolicy.JwtUserPolicy, policy =>
                        policy.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.TrackOwnPrivateRead, policy =>
                        policy.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.TrackEditOwn, policy =>
                        policy.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.TrackCreate, policy =>
                        policy.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.TrackDeleteOwn, policy =>
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
        
        RedisDbMock.Setup(db => db.StringIncrementAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, long value, CommandFlags _) =>
            {
                if (_store.TryGetValue(key, out var currentValue) && long.TryParse(currentValue, out var currentLong))
                {
                    var newValue = currentLong + value;
                    _store[key] = newValue;
                    return newValue;
                }
                else
                {
                    _store[key] = value;
                    return value;
                }
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

        mock.Setup(x => x.GetAsync(It.IsAny<TKey>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
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

