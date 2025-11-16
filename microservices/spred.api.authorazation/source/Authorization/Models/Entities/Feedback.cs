using System.ComponentModel.DataAnnotations;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace Authorization.Models.Entities;

/// <summary>
/// Represents feedback provided by a user, typically including a rating and optional descriptive text.
/// </summary>
public class Feedback : IBaseEntity<Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public Feedback()
    {
        Id = Guid.NewGuid();
    }
    
    /// <summary>
    /// Gets or sets the unique identifier for the feedback entry.
    /// </summary>
    /// <remarks>
    /// This property serves as the primary key for the Feedback entity.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public string? ETag { get; private set;   }
    
    /// <inheritdoc />
    public long Timestamp { get; private set; }

    /// <summary>
    /// Represents the textual content of the feedback provided by a user.
    /// </summary>
    /// <remarks>
    /// The feedback value is a string with a maximum length of 1000 characters.
    /// It captures the detailed opinion, comment, or suggestion shared by the user regarding their experience.
    /// </remarks>
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Represents the type of feedback provided by the user.
    /// </summary>
    [StringLength(50)]
    public string FeedbackType { get; set; } = string.Empty;

    /// <summary>
    /// Represents the content of the feedback.
    /// </summary>
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Represents the unique identifier of the user associated with the feedback.
    /// This property serves as a foreign key and is used for partitioning the data in the database.
    /// </summary>
    [PartitionKey]
    public Guid UserId { get; set; }
}