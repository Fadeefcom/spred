namespace PlaylistService.Models.DTO;

public class PrivateMetadataDto
{
    /// <summary>  
    /// Gets the unique identifier of the playlist.  
    /// </summary>  
    public Guid? Id { get; set; }

    /// <summary>  
    /// Gets the name of the playlist.  
    /// </summary>
    public string? Name { get; init; }

    /// <summary>  
    /// Gets the description of the playlist.  
    /// </summary>
    public string? Description { get; init; }

    /// <summary>  
    /// Gets the dictionary of listen URLs for the playlist.  
    /// </summary>
    public Dictionary<string, string> ListenUrls { get; init; } = [];

    /// <summary>  
    /// Gets the dictionary of submit URLs for the playlist.  
    /// </summary>
    public Dictionary<string, string> SubmitUrls { get; init; } = [];

    /// <summary>
    /// List of playlist tags
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>  
    /// Gets the URL to the tracks of the playlist.  
    /// </summary>
    public string? Href { get; init; }

    /// <summary>  
    /// Gets the URL of the playlist image.  
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>  
    /// Gets the total number of tracks in the playlist.  
    /// </summary>  
    public uint? TracksTotal { get; init; }

    /// <summary>  
    /// Gets or sets the number of followers of the playlist.  
    /// </summary>  
    public uint? Followers { get; set; }
    
    /// <summary>
    /// Number of followers change last 30 days
    /// </summary>
    public int FollowerChange { get; set; }

    /// <summary>  
    /// Gets a value indicating whether the playlist is public.  
    /// </summary>  
    public bool IsPublic { get; init; }

    /// <summary>  
    /// Gets a value indicating whether the playlist is collaborative.  
    /// </summary>  
    public bool Collaborative { get; init; }

    /// <summary>  
    /// Gets the email address for submitting tracks to the playlist.  
    /// </summary>
    public string? SubmitEmail { get; init; }
    
    /// <summary>
    /// Last update at time.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
    
    /// <summary>
    /// Type
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets or sets the platform associated with the playlist.
    /// </summary>
    public string? Platform { get; set; }
};