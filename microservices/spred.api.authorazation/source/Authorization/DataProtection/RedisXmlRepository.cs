using Extensions.Extensions;
using Microsoft.AspNetCore.DataProtection.Repositories;
using StackExchange.Redis;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Authorization.DataProtection;

/// <summary>
/// A Redis-based implementation of the <see cref="IXmlRepository"/> interface for storing and retrieving
/// XML elements used by the ASP.NET Core Data Protection system.
/// </summary>
public class RedisXmlRepository : IXmlRepository
{
    private readonly IDatabase _db;
    private readonly string _dataKey;
    private readonly string _lockKey;
    private readonly ILogger<RedisXmlRepository> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _cacheKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisXmlRepository"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="cache">Local cache storage.</param>
    public RedisXmlRepository(IConnectionMultiplexer redis, ILoggerFactory loggerFactory, IMemoryCache cache)
    {
        _db = redis.GetDatabase();
        _dataKey = "DataProtection-Authorization-Keys";
        _lockKey = $"{_dataKey}-lock";
        _logger = loggerFactory.CreateLogger<RedisXmlRepository>();
        _cache = cache;
        _cacheKey = $"DataProtection:{_dataKey}";
    }

    /// <summary>
    /// Retrieves all XML elements stored in Redis.
    /// </summary>
    /// <returns>A read-only collection of <see cref="XElement"/> objects.</returns>
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        if (_cache.TryGetValue(_cacheKey, out IReadOnlyCollection<XElement>? cached) && cached != null)
            return cached;

        var raw = _db.StringGet(_dataKey);
        if (raw.IsNullOrEmpty) return [];

        try
        {
            var doc = XDocument.Parse(raw!);
            var elements = doc.Root?.Elements().ToList() ?? [];

            var expiration = GetMaxExpiration(elements);
            var ttl = expiration - DateTime.UtcNow;

            if (ttl <= TimeSpan.Zero)
                ttl = TimeSpan.FromMinutes(5); // fallback

            _cache.Set(_cacheKey, elements.AsReadOnly(), ttl);
            return elements;
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("Gat Xml key", "Failed to parse DataProtection keys from Redis.", ex);
            return [];
        }
    }

    /// <summary>
    /// Stores a new XML element in Redis, ensuring thread safety with a Redis lock.
    /// </summary>
    /// <param name="element">The XML element to store.</param>
    /// <param name="friendlyName">A friendly name for the element (not used in this implementation).</param>
    public void StoreElement(XElement element, string friendlyName)
    {
        var lockToken = Guid.NewGuid().ToString();
        var lockAcquired = _db.LockTake(_lockKey, lockToken, TimeSpan.FromSeconds(5));

        if (!lockAcquired)
        {
            _logger.LogSpredWarning("Store Xml key", "Could not acquire Redis lock for DataProtection key store. Skipping key save.");
            return;
        }

        try
        {
            var existing = GetAllElements().ToList();
            existing.Add(element);

            var doc = new XDocument(new XElement("keys", existing));
            _db.StringSet(_dataKey, doc.ToString(SaveOptions.DisableFormatting));

            var expiration = GetMaxExpiration(existing);
            var ttl = expiration - DateTime.UtcNow;

            if (ttl <= TimeSpan.Zero)
                ttl = TimeSpan.FromMinutes(5);

            _cache.Set(_cacheKey, existing.AsReadOnly(), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        }
        finally
        {
            _db.LockRelease(_lockKey, lockToken);
        }
    }
    
    private static DateTime GetMaxExpiration(IEnumerable<XElement> elements)
    {
        DateTime maxExpiration = DateTime.MinValue;
        
        foreach (var element in elements)
        {
            var attr = element.Attribute("expirationDate")?.Value;
            if (DateTime.TryParse(attr, out var d))
            {
                if(d > maxExpiration)
                    maxExpiration = d;
            }
        }
        
        return maxExpiration;
    }
}
