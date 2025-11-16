using System.ComponentModel.DataAnnotations;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace Authorization.Models.Entities;

/// <summary>
/// Represents a request for notification by containing user-provided contact details.
/// </summary>
public class NotifyMe : IBaseEntity<Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public NotifyMe()
    {
        Id = Guid.NewGuid();
    }
    
    /// <summary>
    /// Gets or sets the unique identifier for the "Notify Me" request.
    /// This identifier is used as the primary key in the database.
    /// </summary>
    public Guid Id { get;  private set; }

    /// <inheritdoc />
    public string? ETag { get; private set; }
    
    /// <inheritdoc />
    public long Timestamp { get; private set; }

    /// <summary>
    /// Gets or sets the email address associated with a "Notify Me" request.
    /// </summary>
    /// <remarks>
    /// This property represents the user's contact email, used to receive notifications.
    /// It serves as a core identifier for partitioning in the data storage and must adhere
    /// to the specified maximum length constraint.
    /// </remarks>
    [StringLength(100)] public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Normalized email
    /// </summary>
    [PartitionKey]
    [StringLength(100)]  
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username associated with a "Notify Me" request.
    /// </summary>
    /// <remarks>
    /// This property represents the name of the user in a notification request. It is used
    /// to store and retrieve the user's display name or identifier. The string length for this property
    /// is limited to a maximum of 100 characters.
    /// </remarks>
    [StringLength(100)] public string UserName { get; init; } = string.Empty;
    
    /// <summary>
    /// Artist Role
    /// </summary>
    [StringLength(50)] public string ArtistType { get; init; } = string.Empty;
    
    /// <summary>
    /// Message with in form
    /// </summary>
    [StringLength(500)]public string Message { get; init; } = string.Empty;
}