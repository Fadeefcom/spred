using System.Diagnostics.CodeAnalysis;
using Authorization.Abstractions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Authorization.Services;

/// <summary>
/// Default Cloudinary implementation of <see cref="ICloudinaryWrapper"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public class CloudinaryWrapper : ICloudinaryWrapper
{
    private readonly Cloudinary _cloudinary;

    /// <summary>
    /// Initializes a new instance of <see cref="CloudinaryWrapper"/>.
    /// </summary>
    /// <param name="cloudinary">The Cloudinary SDK client instance.</param>
    public CloudinaryWrapper(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    /// <inheritdoc />
    public Task<ImageUploadResult> UploadAsync(ImageUploadParams uploadParams, CancellationToken cancellationToken = default) =>
        _cloudinary.UploadAsync(uploadParams, cancellationToken);

    /// <inheritdoc />
    public Task<DeletionResult> DestroyAsync(DeletionParams deletionParams, CancellationToken cancellationToken = default) =>
        _cloudinary.DestroyAsync(deletionParams);
}