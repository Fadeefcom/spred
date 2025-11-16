namespace TrackService.Helpers;

/// <summary>
/// Provides utility methods for reading and saving byte files.
/// </summary>
public static class ByteFileReader
{
    /// <summary>
    /// Asynchronously saves the provided form file to the disk.
    /// </summary>
    /// <param name="formFile">The form file to save.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path of the saved file.</returns>
    public static async Task<string> SaveFile(IFormFile formFile)
    {
        string output = Path.Combine(Environment.CurrentDirectory, Models.Names.AudioFiles, formFile.FileName);

        await using var fileStream = new FileStream(output, FileMode.OpenOrCreate);
        await formFile.CopyToAsync(fileStream);
        formFile.OpenReadStream().Position = 0;

        return output;
    }
}
