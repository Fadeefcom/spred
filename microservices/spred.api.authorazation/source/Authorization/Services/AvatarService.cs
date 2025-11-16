using Authorization.Abstractions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Authorization.Services;

/// <summary>
/// Cloudinary-based implementation of <see cref="IAvatarService"/>.
/// </summary>
public class AvatarService : IAvatarService
{
    private readonly ICloudinaryWrapper _cloudinary;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="cloudinaryWrapper"></param>
    public AvatarService(ICloudinaryWrapper cloudinaryWrapper)
    {
        _cloudinary = cloudinaryWrapper;
    }

    /// <inheritdoc />
    public async Task<string> SaveAvatarAsync(string userId, Stream fileStream, string contentType, CancellationToken cancellationToken)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{userId}.png", fileStream),
            Folder = $"avatars/{userId}",
            PublicId = Guid.NewGuid().ToString(),
            Overwrite = false,
            Transformation = new Transformation().Width(400).Height(400).Crop("fill").Gravity("face")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            throw new InvalidOperationException("Upload to Cloudinary failed");

        return uploadResult.SecureUrl.ToString();
    }

    /// <inheritdoc />
    public async Task DeleteAvatarAsync(string userId, string avatarUrl, CancellationToken cancellationToken)
    {
        var publicId = GetPublicIdFromUrl(avatarUrl);
        if (string.IsNullOrEmpty(publicId)) return;

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams, cancellationToken);
    }

    private static string? GetPublicIdFromUrl(string avatarUrl)
    {
        try
        {
            var uri = new Uri(avatarUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var fileName = Path.GetFileNameWithoutExtension(segments.Last());
            return string.Join("/", segments.SkipWhile(s => s != "avatars").Skip(1).Prepend("avatars")) + "/" + fileName;
        }
        catch
        {
            return null;
        }
    }
}
