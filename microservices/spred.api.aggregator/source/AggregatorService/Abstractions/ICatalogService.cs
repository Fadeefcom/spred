namespace AggregatorService.Abstractions;

public interface ICatalogService
{
    public Task CatalogAggregateReport(int bucket, Guid id, string type, string shortDate);
}