using ActivityService.Models;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Components;
using Spred.Bus.Contracts;

namespace ActivityService.Components.Consumers;

/// <summary>
/// The ActivityConsumer class is responsible for consuming messages of type
/// <see cref="ActivityRecord"/> and processing them to create new activity entities
/// in a Cosmos DB container.
/// </summary>
/// <remarks>
/// This class implements the <see cref="MassTransit.IConsumer{ActivityRecord}"/> interface
/// and is intended to listen for messages from a message broker. Upon receiving a message,
/// it processes the data and stores it as an instance of <see cref="ActivityEntity"/> in a Cosmos DB container.
/// </remarks>
public sealed class ActivityConsumer : IConsumer<ActivityRecord>
{
    /// <summary>
    /// Represents the Azure Cosmos DB <see cref="Container"/> instance used for interacting with the data store.
    /// This container is tied to the specified entity type and operations for the <see cref="ActivityEntity"/> objects.
    /// </summary>
    /// <remarks>
    /// Acts as the entry point for data queries, insertions, and updates within the Azure Cosmos database specific
    /// to activities. This instance encapsulates the physical database table or 'container' in Cosmos terminology.
    /// </remarks>
    private readonly Container _container;

    /// <summary>
    /// Consumer for processing activity records.
    /// </summary>
    /// <remarks>
    /// This consumer is responsible for handling incoming messages of type <see cref="ActivityRecord"/> and processing them.
    /// It interacts with an Azure Cosmos DB container via the provided <see cref="CosmosContainer{ActivityEntity}"/> to store or manipulate the data.
    /// </remarks>
    public ActivityConsumer(CosmosContainer<ActivityEntity> container)
    {
        _container = container.Container;
    }

    /// Consumes the provided activity record message, processes it, and stores it in the database as an activity entity.
    /// <param name="context">
    /// The consume context that contains the activity record message to be processed.
    /// </param>
    /// <return>
    /// A task that represents the asynchronous operation of processing and storing the activity record.
    /// </return>
    public async Task Consume(ConsumeContext<ActivityRecord> context)
    {
        var record = context.Message;

        var query = new QueryDefinition(
            "SELECT VALUE MAX(c.Sequence) FROM c");
        
        var partitionKey = new PartitionKeyBuilder().Add(record.OwnerUserId.ToString()).Add(record.ObjectId.ToString()).Build();

        var iterator = _container.GetItemQueryIterator<int?>(query, requestOptions: new QueryRequestOptions()
        {
            PartitionKey = partitionKey
        });
        
        var maxSeq = 0;
        while (iterator.HasMoreResults)
        {
            foreach (var val in await iterator.ReadNextAsync())
                maxSeq = val ?? 0;
        }

        var entity = new ActivityEntity
        {
            Id = record.Id,
            OwnerUserId = record.OwnerUserId,
            OtherPartyUserId = record.OtherPartyUserId,
            ObjectType = record.ObjectType,
            ObjectId = record.ObjectId,
            Verb = record.Verb,
            MessageKey = record.MessageKey,
            Args = record.MessageArgs,
            Before = record.Before,
            After = record.After,
            Audience = record.Audience,
            Sequence = maxSeq + 1,
            CreatedAt = record.CreatedAt,
            CorrelationId = record.CorrelationId,
            Service = record.Service,
            Importance = record.Importance,
            Tags = record.Tags,
            ActorUserId = record.ActorUserId
        };

        await _container.CreateItemAsync(entity, partitionKey);
    }
}