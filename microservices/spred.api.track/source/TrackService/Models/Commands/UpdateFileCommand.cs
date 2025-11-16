using System.Data.Common;
using MediatR;

namespace TrackService.Models.Commands;

/// <summary>
/// Command to update a file associated with a specific user.
/// </summary>
public class UpdateFileCommand : INotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateFileCommand"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the file to update.</param>
    /// <param name="spredUserId">The unique identifier of the user associated with the file.</param>
    /// <param name="file">The new file to update.</param>
    public UpdateFileCommand(Guid id, Guid spredUserId, IFormFile file)
    {
        Id = id;
        File = file;
        SpredUserId = spredUserId;
    }

    /// <summary>
    /// Gets the unique identifier of the file to update.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user associated with the file.
    /// </summary>
    public Guid SpredUserId { get; private set; }

    /// <summary>
    /// Gets the new file to update.
    /// </summary>
    public IFormFile File { get; private set; }
}
