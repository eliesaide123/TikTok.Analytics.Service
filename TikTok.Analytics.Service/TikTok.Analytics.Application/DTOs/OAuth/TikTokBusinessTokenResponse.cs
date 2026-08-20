using System.Text.Json;
using System.Text.Json.Serialization;

namespace TikTok.Analytics.Application.DTOs.OAuth;

/// <summary>
/// Response from the business token endpoint. Business API responses wrap their payload in
/// code/message/data and report failure as code != 0 with HTTP 200, so the envelope decides
/// success, not the status line.
///
/// Fields are nullable because the exact contract for the Accounts product is unverified —
/// see <see cref="Extra"/> for whatever is not modelled here.
/// </summary>
public class TikTokBusinessTokenResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("data")]
    public TikTokBusinessTokenData? Data { get; set; }

    public bool IsSuccess => Code == 0 && !string.IsNullOrEmpty(Data?.AccessToken);
}

public class TikTokBusinessTokenData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public long? RefreshTokenExpiresIn { get; set; }

    /// <summary>May arrive as a string or an array of strings depending on the product.</summary>
    [JsonPropertyName("scope")]
    public JsonElement? Scope { get; set; }

    // Candidate carriers for the account identifier. Which one is populated depends on the
    // product granted, so all are captured and the first non-empty wins.
    [JsonPropertyName("business_id")]
    public string? BusinessId { get; set; }

    [JsonPropertyName("business_ids")]
    public List<string>? BusinessIds { get; set; }

    [JsonPropertyName("advertiser_ids")]
    public List<string>? AdvertiserIds { get; set; }

    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>Anything the properties above did not claim.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

    /// <summary>First identifier that looks usable as /business/get/?business_id=.</summary>
    public string? ResolveBusinessId() =>
        new[] { BusinessId, BusinessIds?.FirstOrDefault(), AdvertiserIds?.FirstOrDefault(), OpenId }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    public string ScopeAsString() => Scope is null
        ? string.Empty
        : Scope.Value.ValueKind == JsonValueKind.Array
            ? string.Join(",", Scope.Value.EnumerateArray().Select(e => e.ToString()))
            : Scope.Value.ToString();
}
