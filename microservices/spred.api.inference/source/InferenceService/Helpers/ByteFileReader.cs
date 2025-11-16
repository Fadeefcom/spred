using InferenceService.Models;
using System.Diagnostics;

namespace InferenceService.Helpers;

/// <summary>
/// Provides methods for reading and saving byte files.
/// </summary>
public static class ByteFileReader
{
    /// <summary>
    /// Reads the bytes from the provided form file asynchronously.
    /// </summary>
    /// <param name="formFile">The form file to read bytes from.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the byte array of the file.</returns>
    public static async Task<byte[]> GetFileBytesAsync(IFormFile formFile)
    {
        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Opens a read stream from the provided byte array.
    /// </summary>
    /// <param name="fileBytes">The byte array to create a stream from.</param>
    /// <returns>A memory stream containing the file bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the byte array is null or empty.</exception>
    public static MemoryStream OpenReadStreamFromBytes(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length == 0)
            throw new ArgumentNullException(nameof(fileBytes));

        return new MemoryStream(fileBytes);
    }

    /// <summary>
    /// Saves the provided form file to the disk asynchronously.
    /// </summary>
    /// <param name="formFile">The form file to save.</param>
    /// <param name="activity">The activity associated with the file save operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path of the saved file.</returns>
    public static async Task<string> SaveFile(IFormFile formFile, Activity? activity)
    {
        var output = Path.Combine(Environment.CurrentDirectory, Names.AudioFiles, Path.GetRandomFileName());

        await using var fileStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None);
        await formFile.CopyToAsync(fileStream);

        return output;
    }

    /// <summary>
    /// Saves the provided stream to the disk asynchronously.
    /// </summary>
    /// <param name="stream">The stream to save.</param>
    /// <param name="activity">The activity associated with the file save operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path of the saved file.</returns>
    public static async Task<string> SaveFile(Stream stream, Activity? activity)
    {
        var output = Path.Combine(Environment.CurrentDirectory, Names.AudioFiles, Path.GetRandomFileName());

        await using var fileStream = new FileStream(output, FileMode.OpenOrCreate);
        await stream.CopyToAsync(fileStream);

        return output;
    }
}
