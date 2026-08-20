using TikTok.Analytics.Application.Configuration;

namespace TikTok.Analytics.Application.Interfaces;

/// <summary>A resolved Business API credential: the token and the account it addresses.</summary>
public record BusinessCredential(string AccessToken, string BusinessId);

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

    /// <summary>
    /// Returns a usable Business API credential for the page, or null if none can be obtained.
    ///
    /// Same precedence as the Display side: the business token store wins, and the values
    /// configured on the page remain a fallback so a manually issued token can be dropped
    /// into appsettings for a first test before the OAuth flow is used.
    /// </summary>
    Task<BusinessCredential?> GetBusinessCredentialAsync(PageConfig page, CancellationToken ct = default);
}
