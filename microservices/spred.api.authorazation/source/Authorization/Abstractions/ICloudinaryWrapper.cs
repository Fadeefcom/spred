using Authorization.Services;
using CloudinaryDotNet.Actions;

namespace Authorization.Abstractions;

/// <summary>
/// Abstraction for Cloudinary operations used by <see cref="AvatarService"/>.
/// </summary>
public interface ICloudinaryWrapper
{
    /// <summary>
    /// Uploads an image to Cloudinary.
    /// </summary>
    /// <param name="uploadParams">Parameters describing the upload.</param>
    /// <param name="cancellationToken">Token for cancelling the async operation.</param>
    /// <returns>Result of the upload, including status and URL.</returns>
    Task<ImageUploadResult> UploadAsync(ImageUploadParams uploadParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from Cloudinary.
    /// </summary>
    /// <param name="deletionParams">Parameters describing the deletion.</param>
    /// <param name="cancellationToken">Token for cancelling the async operation.</param>
    /// <returns>Result of the deletion operation.</returns>
    Task<DeletionResult> DestroyAsync(DeletionParams deletionParams, CancellationToken cancellationToken = default);
}