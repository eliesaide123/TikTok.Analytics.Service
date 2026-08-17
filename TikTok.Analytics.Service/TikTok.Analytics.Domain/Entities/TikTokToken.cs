namespace TikTok.Analytics.Domain.Entities;

/// <summary>
/// A single account's OAuth credentials. Keyed by OpenId, which is TikTok's stable
/// per-app user identifier and the only account handle returned by the token endpoint.
/// </summary>
public class TikTokToken
{
    public string OpenId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public string Scope { get; set; } = string.Empty;

    /// <summary>Access tokens last 24h.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Refresh tokens last 365 days. Past this, the user must re-authorize interactively.</summary>
    public DateTime RefreshExpiresAtUtc { get; set; }

    public DateTime ObtainedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set from the page config once the OpenId is matched to a known page.</summary>
    public string? PageId { get; set; }

    public bool IsAccessExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsRefreshExpired(DateTime nowUtc) => nowUtc >= RefreshExpiresAtUtc;

    public bool NeedsRefresh(DateTime nowUtc, int withinMinutes) =>
        nowUtc.AddMinutes(withinMinutes) >= ExpiresAtUtc && !IsRefreshExpired(nowUtc);
}
