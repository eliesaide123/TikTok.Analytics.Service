using System.Text.Json.Serialization;

namespace TikTok.Analytics.Application.DTOs.OAuth;

/// <summary>
/// Response from POST https://open.tiktokapis.com/v2/oauth/token/ — used for both
/// the authorization_code exchange and the refresh_token grant.
/// </summary>
public class TikTokTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_expires_in")]
    public long RefreshExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    // The token endpoint reports failures inline with a 200, so these decide success.
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("log_id")]
    public string? LogId { get; set; }

    public bool IsSuccess => string.IsNullOrEmpty(Error) && !string.IsNullOrEmpty(AccessToken);
}
