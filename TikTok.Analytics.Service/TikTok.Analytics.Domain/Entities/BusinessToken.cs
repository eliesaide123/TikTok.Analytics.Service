namespace TikTok.Analytics.Domain.Entities;

/// <summary>
/// Credentials for the TikTok API for Business, kept separate from <see cref="TikTokToken"/>:
/// different authorization server, different header (Access-Token, not Bearer), and keyed by
/// BusinessId rather than OpenId.
/// </summary>
public class BusinessToken
{
    /// <summary>
    /// The per-account identifier that /business/get/ expects. Note this is NOT the
    /// Business Center ID, which identifies the owning organisation instead.
    /// </summary>
    public string BusinessId { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Present only if TikTok issued one; business tokens do not always rotate.</summary>
    public string? RefreshToken { get; set; }

    public string Scope { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RefreshExpiresAtUtc { get; set; }

    public DateTime ObtainedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Which configured page this credential belongs to.</summary>
    public string? PageId { get; set; }

    /// <summary>
    /// Every non-secret field TikTok returned at authorization time. The exact response
    /// shape is unverified, so this preserves anything the typed properties missed —
    /// particularly whichever field really carries the business id.
    /// </summary>
    public Dictionary<string, string> RawFields { get; set; } = new();

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
}
