using Extensions.Extensions;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace AggregatorService.BackgroundTasks;

public class DailyPlaylistCronTask : BackgroundService
{
    private readonly ILogger<DailyPlaylistCronTask> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private TimeSpan _runAtTime;
    private readonly string _databaseName;
    private readonly bool _runOnStart;
    private readonly IDatabase _database;

    private const string ResetKey = "chartmetrics:rate:reset";
    private const int MaxPlaylistsPerRun = 2000;

    public DailyPlaylistCronTask(
        ILogger<DailyPlaylistCronTask> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IConnectionMultiplexer multiplexer)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _databaseName = configuration["DbConnectionOptions:DatabaseName"]
                        ?? throw new ArgumentException("Database name not configured.");
        _database = multiplexer.GetDatabase();

        _runAtTime = TimeSpan.TryParse(configuration.GetSection("DailyPlaylistCronTask")["CronTime"] ?? "03:00:00", out var span)
            ? span
            : new TimeSpan(3, 0, 0);

        _runOnStart = bool.TryParse(configuration.GetSection("DailyPlaylistCronTask")["RunOnStart"], out var run) && run;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_runOnStart)
        {
            _logger.LogSpredInformation("DailyPlaylistCronTaskExecuteAsync", "Running playlist task immediately on start.");
            await RunTaskAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var todayRun = now.Date.Add(_runAtTime);
            var nextRun = now <= todayRun ? todayRun : now.Date.AddDays(1).Add(_runAtTime);

            var resetEpochStr = await _database.StringGetAsync(ResetKey);
            if (resetEpochStr.HasValue && long.TryParse(resetEpochStr.ToString(), out var resetEpoch))
            {
                var resetAt = DateTimeOffset.FromUnixTimeSeconds(resetEpoch).UtcDateTime;

                if (resetAt > now)
                    nextRun = resetAt;
            }

            var delay = nextRun - now;

            _logger.LogSpredInformation("DailyPlaylistCronTaskExecuteAsync",
                $"Next daily playlist task scheduled at {nextRun:O}, delay {delay}");
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            await RunTaskAsync(stoppingToken);
            _runAtTime = DateTimeOffset.UtcNow.TimeOfDay.Add(TimeSpan.FromMinutes(5));
            await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
        }
    }

    private async Task RunTaskAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogSpredInformation("DailyPlaylistCronTaskStarted", "Running playlist task.");
            using var scope = _scopeFactory.CreateScope();
            var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
            var catalogContainer = cosmosClient.GetContainer(_databaseName, "CatalogMetadata_v2");
            var sendEndpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

            var monthAgo = DateTime.UtcNow.AddMonths(-1).ToString("O");

            var query = new QueryDefinition(@"
                SELECT * FROM c
                WHERE (c.Type = @recordlabelType OR c.Type = 'radioMetadata')
                  AND c.UpdateAt < @monthAgo
                ORDER BY c.UpdateAt ASC
            ")
                .WithParameter("@recordlabelType", "radio")
                .WithParameter("@monthAgo", monthAgo);

            var playlists = new List<JObject>();
            
            using var iterator = catalogContainer.GetItemQueryIterator<JObject>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(Guid.Empty.ToString()),
                    MaxItemCount = MaxPlaylistsPerRun
                }
            );

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                playlists.AddRange(response);
            }

            foreach (var playlist in playlists)
            {
                var type = playlist["Type"]!.ToString();
                var primaryIdRaw = playlist["PrimaryId"]?.ToString() ?? string.Empty;
                var primaryIdParts = primaryIdRaw.Split(':');

                var platform = primaryIdParts.ElementAtOrDefault(0) ?? string.Empty;

                if (!bool.TryParse(playlist["NeedUpdateStatInfo"]?.ToString(), out var updateStats))
                    updateStats = true;

                var message = new CatalogEnrichmentRequest
                {
                    ChartmetricsId = playlist["ChartmetricsId"]?.ToString() ?? string.Empty,
                    SoundChartsApi = playlist["SoundChartsId"]?.ToString() ?? string.Empty,
                    UpdateStatsInfo = updateStats,
                    Platform = platform,
                    Id = Guid.TryParse(playlist["id"]?.ToString(), out var id) ? id : Guid.Empty,
                    PrimaryId = primaryIdRaw,
                    SpredUserId = Guid.Parse(playlist["SpredUserId"]?.ToString()!),
                    Type = type
                };

                if (string.IsNullOrWhiteSpace(message.PrimaryId) || message.Id == Guid.Empty)
                {
                    _logger.LogSpredWarning("DailyPlaylistCronTaskSkipping",
                        $"Skipping CatalogUpdateRequest due to missing or invalid identifiers. " +
                        $"PrimaryId: {message.PrimaryId}, Id: {message.Id}");
                    return;
                }

                var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("exchange:catalog-enrichment-request"));
                await endpoint.Send(message, CancellationToken.None);
            }

            _logger.LogSpredInformation("DailyPlaylistCronTaskPushed",
                $"Daily playlist task pushed {playlists.Count} playlists");
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("DailyPlaylistCronTaskError", "Failed to run daily playlist cron task", ex);
        }
    }
}
