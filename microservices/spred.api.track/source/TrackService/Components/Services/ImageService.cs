namespace TrackService.Components.Services;

/// <summary>
/// Image service
/// </summary>
public static class ImageService
{
    private static readonly string[] _images =
    [
        "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MXx8bXVzaWN8ZW58MHx8MHx8fDI%3D",
        "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NXx8bXVzaWN8ZW58MHx8MHx8fDI%3D",
        "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?q=80&w=2940&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        "https://images.unsplash.com/photo-1525362081669-2b476bb628c3?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjJ8fG11c2ljfGVufDB8fDB8fHwy",
        "https://images.unsplash.com/photo-1483000805330-4eaf0a0d82da?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MzR8fG11c2ljfGVufDB8fDB8fHwy",
        "https://images.unsplash.com/photo-1619983081593-e2ba5b543168?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDN8fG11c2ljfGVufDB8fDB8fHwy",
        "https://images.unsplash.com/photo-1484876065684-b683cf17d276?w=900&auto=format&fit=crop&q=60&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mzh8fG11c2ljfGVufDB8fDB8fHwy"
    ];


    /// <summary>
    /// Get random image urls
    /// </summary>
    /// <returns></returns>
    public static async Task<string> GetFinalImageUrlAsync()
    {
        var index = Random.Shared.Next(_images.Length);
        var image = _images[index];
        return await Task.FromResult(image);
    }

}