using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Authorization.Options;
using Newtonsoft.Json;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace Authorization.Models.Entities;

/// <summary>
/// Represents an OAuth user authentication entry, containing necessary details about the user's authorization through an external provider.
/// </summary>
public class OAuthAuthentication : IBaseEntity<Guid>
{
    public OAuthAuthentication()
    {
        Id = Guid.NewGuid();
    }
    
    /// <summary>
    /// Gets the unique identifier for the OAuth authentication record.
    /// </summary>
    /// <remarks>
    /// This property serves as the primary key for the OAuthAuthentication entity and is automatically
    /// generated as a new GUID when a record is created. It uniquely identifies each instance of
    /// OAuthAuthentication in the data store.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public string? ETag { get;  private set;  }
    
    /// <inheritdoc />
    public long Timestamp { get;  private set;  }

    /// <summary>
    /// Gets or initializes the unique identifier associated with a specific user in the Spred system.
    /// </summary>
    /// <remarks>
    /// This property serves as a foreign key that links OAuth authentication records with user entities.
    /// It ensures proper association between users and their OAuth authentication data.
    /// </remarks>
    public required Guid SpredUserId { get; init; }

    /// <summary>
    /// Gets or initializes the primary identifier associated with the OAuth authentication entry.
    /// This identifier represents the unique ID provided by the external OAuth provider.
    /// </summary>
    /// <remarks>
    /// The <c>PrimaryId</c> is a required field with a maximum length of 100 characters.
    /// It is used as a partition key in the database for ensuring efficient querying and storage operations.
    /// </remarks>
    [MaxLength(100)]
    [PartitionKey(0)]
    public required string PrimaryId { get; init; }

    /// <summary>
    /// Gets the type of OAuth authentication used for this record.
    /// The OAuthType property represents the provider or service (e.g., Base, Spotify, Google, or Yandex)
    /// that the specific user authentication is associated with. It is a required field.
    /// </summary>
    [PartitionKey(1)]
    public required string OAuthProvider { get; init; }

    /// <summary>
    /// Gets the date and time when the OAuth authentication record was added.
    /// </summary>
    /// <remarks>
    /// The property is automatically initialized to the current date and time during the creation of the instance.
    /// </remarks>
    public DateTime DateAdded { get; } = DateTime.Now;
}
