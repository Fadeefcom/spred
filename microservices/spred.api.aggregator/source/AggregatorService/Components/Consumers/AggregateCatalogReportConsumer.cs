using AggregatorService.Abstractions;
using Extensions.Extensions;
using MassTransit;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace AggregatorService.Components.Consumers;

/// <summary>
/// Consumer
/// </summary>
public class AggregateCatalogReportConsumer : IConsumer<AggregateCatalogReport>
{
    private readonly ICatalogService _catalogService;
    private readonly IDatabase _redis;
    private readonly ILogger<AggregateCatalogReportConsumer> _logger;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="catalogService"></param>
    /// <param name="redis"></param>
    /// <param name="loggerFactory"></param>
    public AggregateCatalogReportConsumer(ICatalogService catalogService, IConnectionMultiplexer redis, ILoggerFactory loggerFactory)
    {
        _catalogService = catalogService;
        _redis = redis.GetDatabase();
        _logger = loggerFactory.CreateLogger<AggregateCatalogReportConsumer>();
    }

    public async Task Consume(ConsumeContext<AggregateCatalogReport> context)
    {
        var message = context.Message;
        string lockKey = $"catalog:inference:lock:{message.Type}:{message.Bucket}:{message.Data}";

        bool acquired = await _redis.StringSetAsync(new RedisKey(lockKey), new RedisValue("1"), TimeSpan.FromDays(1), When.NotExists);
        
        if (!acquired)
        {
            _logger.LogSpredDebug("AggregateCatalogReportConsumer",$"Skip processing {message.Id}, already in progress.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _catalogService.CatalogAggregateReport(message.Bucket, message.Id, message.Type, message.Data);
            }
            catch (System.Exception ex)
            {
                _logger.LogSpredError($"Error during enrichment for {message.Id}: {ex.Message}", ex);
            }
        });

        await Task.CompletedTask;
    }
}