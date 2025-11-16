using System.Net;
using Microsoft.Azure.Cosmos;

namespace AggregatorService.Test.Helpers;

public class FakeFeedResponse<T> : FeedResponse<T>
{
    private readonly List<T> _items;

    public FakeFeedResponse(IEnumerable<T> items)
    {
        _items = items.ToList();
    }

    public override IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    public override int Count => _items.Count;
    public override string IndexMetrics { get; }
    public override string ContinuationToken => null;
    public override Headers Headers => new Headers();
    public override IEnumerable<T> Resource { get; }
    public override HttpStatusCode StatusCode { get; }
    public override string ActivityId => Guid.NewGuid().ToString();
    public override CosmosDiagnostics Diagnostics { get; }
    public override double RequestCharge => 1.0;
}