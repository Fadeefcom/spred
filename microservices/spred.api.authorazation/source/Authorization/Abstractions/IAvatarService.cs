namespace Authorization.Abstractions;

/// <summary>
/// Defines a service for working with user avatars.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Saves the avatar for a user in persistent storage.
    /// </summary>
    /// <param name="userId">Unique identifier of the user who owns the avatar.</param>
    /// <param name="fileStream">Input stream containing the image file to upload.</param>
    /// <param name="contentType">MIME type of the image (e.g., image/png, image/jpeg).</param>
    /// <param name="cancellationToken">Cancellation token for request abortion.</param>
    /// <returns>
    /// A task that resolves to the public URL of the uploaded avatar.
    /// </returns>
    Task<string> SaveAvatarAsync(string userId, Stream fileStream, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the avatar for a user from persistent storage.
    /// </summary>
    /// <param name="userId">Unique identifier of the user.</param>
    /// <param name="avatarUrl">The URL of the avatar to delete.</param>
    /// <param name="cancellationToken">Cancellation token for request abortion.</param>
    /// <returns>
    /// A task that completes when the avatar is deleted.
    /// </returns>
    Task DeleteAvatarAsync(string userId, string avatarUrl, CancellationToken cancellationToken);
}
