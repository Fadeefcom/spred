using System.Text.Json.Serialization;
using Authorization.Options;

namespace Authorization.Models.Dto;

/// <summary>
/// Data Transfer Object (DTO) representing an OAuth authentication record.
/// </summary>
internal sealed record OAuthAuthenticationDto
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public OAuthAuthenticationDto()
    {}
    
    /// <summary>
    /// Json constructor
    /// </summary>
    [JsonConstructor]
    private OAuthAuthenticationDto(Guid id, 
        Guid spredUserId, string primaryId, 
        string oauthProvider, string accessToken, 
        DateTime dateAdded)
    {
        Id = id;
        SpredUserId = spredUserId;
        PrimaryId = primaryId;
        OAuthProvider = oauthProvider;
        AccessToken = accessToken;
        DateAdded = dateAdded;
    }
    
    public Guid Id { get; init; }
    
    public required Guid SpredUserId { get; init; }
    
    public required string PrimaryId { get; init; }

    public required string OAuthProvider { get; init; }

    public string? AccessToken { get; set; }
    
    public DateTime DateAdded { get; init; }
}