using Microsoft.Extensions.Logging;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Services;

/// <summary>
/// Decides which Display API credential a page should use at ingestion time.
///
/// The OAuth store is authoritative when it holds a token for the page. The configured
/// token remains a fallback so that pages not yet migrated through /api/auth/tiktok/login
/// keep working unchanged.
/// </summary>
public class TikTokTokenResolver : ITikTokTokenResolver
{
    private readonly ITikTokTokenStore _tokenStore;
    private readonly ITikTokOAuthClient _oauthClient;
    private readonly ILogger<TikTokTokenResolver> _logger;

    public TikTokTokenResolver(
        ITikTokTokenStore tokenStore,
        ITikTokOAuthClient oauthClient,
        ILogger<TikTokTokenResolver> logger)
    {
        _tokenStore = tokenStore;
        _oauthClient = oauthClient;
        _logger = logger;
    }

    public async Task<string?> GetDisplayAccessTokenAsync(PageConfig page, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var stored = (await _tokenStore.GetAllAsync(ct))
            .FirstOrDefault(t => string.Equals(t.PageId, page.PageId, StringComparison.Ordinal));

        if (stored is null)
            return FallBackToConfig(page, "no OAuth token is stored for this page");

        if (!stored.IsAccessExpired(now))
            return stored.AccessToken;

        // Expired. The scheduled refresh job may not have run since it lapsed, so try now.
        if (stored.IsRefreshExpired(now))
        {
            _logger.LogError(
                "Page {PageId}: the refresh token expired on {ExpiredAt:u}. Re-authorize at /api/auth/tiktok/login",
                page.PageId, stored.RefreshExpiresAtUtc);
            return FallBackToConfig(page, "the stored refresh token has expired");
        }

        try
        {
            _logger.LogInformation("Page {PageId}: stored access token expired, refreshing before ingestion", page.PageId);

            var refreshed = await _oauthClient.RefreshAsync(stored.RefreshToken, ct);
            if (string.IsNullOrEmpty(refreshed.OpenId))
                refreshed.OpenId = stored.OpenId;
            refreshed.PageId = stored.PageId;

            await _tokenStore.SaveAsync(refreshed, ct);
            return refreshed.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Page {PageId}: on-demand token refresh failed", page.PageId);
            return FallBackToConfig(page, "the on-demand refresh failed");
        }
    }

    private string? FallBackToConfig(PageConfig page, string reason)
    {
        if (!string.IsNullOrWhiteSpace(page.DisplayAccessToken))
        {
            _logger.LogInformation(
                "Page {PageId}: using the DisplayAccessToken from configuration because {Reason}.",
                page.PageId, reason);
            return page.DisplayAccessToken;
        }

        _logger.LogWarning(
            "Page {PageId}: no Display API token available ({Reason}) and no configured fallback. " +
            "Authorize this page at /api/auth/tiktok/login?pageId={PageId}",
            page.PageId, reason);
        return null;
    }
}
