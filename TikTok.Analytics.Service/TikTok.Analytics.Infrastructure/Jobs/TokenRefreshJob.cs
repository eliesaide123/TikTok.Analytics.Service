using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Jobs;

/// <summary>
/// Keeps stored access tokens alive. TikTok access tokens expire after 24 hours, so this
/// runs well inside that window and refreshes anything close to expiry.
/// </summary>
[DisallowConcurrentExecution]
public class TokenRefreshJob : IJob
{
    private readonly ITikTokTokenStore _tokenStore;
    private readonly ITikTokOAuthClient _oauthClient;
    private readonly TikTokOAuthOptions _options;
    private readonly ILogger<TokenRefreshJob> _logger;

    public TokenRefreshJob(
        ITikTokTokenStore tokenStore,
        ITikTokOAuthClient oauthClient,
        IOptions<TikTokOAuthOptions> options,
        ILogger<TokenRefreshJob> logger)
    {
        _tokenStore = tokenStore;
        _oauthClient = oauthClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;
        var tokens = await _tokenStore.GetAllAsync(ct);

        if (tokens.Count == 0)
        {
            _logger.LogInformation("Token refresh: nothing stored yet. Authorize an account at /api/auth/tiktok/login");
            return;
        }

        var refreshed = 0;
        var failed = 0;

        foreach (var token in tokens)
        {
            if (token.IsRefreshExpired(now))
            {
                // Nothing to be done automatically — the 365-day refresh token is gone.
                _logger.LogWarning("Refresh token for open_id {OpenId} expired on {ExpiredAt:u}. " +
                                   "This account must re-authorize at /api/auth/tiktok/login",
                    token.OpenId, token.RefreshExpiresAtUtc);
                failed++;
                continue;
            }

            if (!token.NeedsRefresh(now, _options.RefreshIfExpiringWithinMinutes))
                continue;

            try
            {
                var updated = await _oauthClient.RefreshAsync(token.RefreshToken, ct);

                // The refresh response echoes open_id, but keep the known one if it comes back blank,
                // and carry the page mapping across.
                if (string.IsNullOrEmpty(updated.OpenId))
                    updated.OpenId = token.OpenId;
                updated.PageId = token.PageId;

                await _tokenStore.SaveAsync(updated, ct);
                refreshed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for open_id {OpenId}", token.OpenId);
                failed++;
            }
        }

        _logger.LogInformation("Token refresh complete. Stored: {Total}, refreshed: {Refreshed}, failed: {Failed}",
            tokens.Count, refreshed, failed);
    }
}
