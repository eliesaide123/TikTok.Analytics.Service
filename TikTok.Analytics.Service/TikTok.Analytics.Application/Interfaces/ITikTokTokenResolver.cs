using TikTok.Analytics.Application.Configuration;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokTokenResolver
{
    /// <summary>
    /// Returns a usable Display API access token for the page, or null if none can be obtained.
    ///
    /// Resolution order: the OAuth token store first (refreshing on the spot if the stored
    /// token has expired), then the page's configured token as a fallback. Returning null
    /// means the caller should skip Display ingestion for this page rather than call the API
    /// with an empty credential.
    /// </summary>
    Task<string?> GetDisplayAccessTokenAsync(PageConfig page, CancellationToken ct = default);
}
