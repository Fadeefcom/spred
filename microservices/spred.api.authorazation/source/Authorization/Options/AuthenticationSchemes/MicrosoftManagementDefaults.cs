namespace Authorization.Options.AuthenticationSchemes;

/// <summary>
/// Contains default values and endpoints for Microsoft OAuth authentication
/// used specifically for management or administrative access (e.g., RabbitMQ, internal tools).
/// This scheme is isolated from the main platform authentication.
/// </summary>
public static class MicrosoftManagementDefaults
{
    /// <summary>
    /// Unique authentication scheme name for Microsoft management access.
    /// </summary>
    public const string AuthenticationScheme = "MicrosoftManagement";

    /// <summary>
    /// Microsoft OAuth 2.0 authorization endpoint for user login.
    /// </summary>
    public const string AuthorizationEndpoint = "https://login.microsoftonline.com/9af483bb-489a-4f75-bfb4-8de391e183a5/oauth2/v2.0/authorize";

    /// <summary>
    /// Microsoft OAuth 2.0 token endpoint for exchanging authorization codes for tokens.
    /// </summary>
    public const string TokenEndpoint = "https://login.microsoftonline.com/9af483bb-489a-4f75-bfb4-8de391e183a5/oauth2/v2.0/token";

    /// <summary>
    /// Microsoft Graph API endpoint to retrieve user profile information.
    /// </summary>
    public const string UserInformationEndpoint = "https://graph.microsoft.com/v1.0/me";

    /// <summary>
    /// Contains the list of allowed email addresses for Microsoft management authentication.
    /// Only users with emails from this list will be granted access to management resources
    /// (e.g., RabbitMQ management UI, internal admin panels).
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedEmails = ["gleb@spred.com"];
}