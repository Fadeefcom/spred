using Azure;
using Moq;

namespace TrackService.Test.Helpers;

public class MockAsyncPageable<T> : AsyncPageable<T>
{
    private readonly IEnumerable<T> _items;

    public MockAsyncPageable(IEnumerable<T> items)
    {
        _items = items;
    }

    public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
    {
        var page = Page<T>.FromValues(_items.ToList(), null, new Mock<Response>().Object);
        yield return page;
        await Task.CompletedTask;
    }
}