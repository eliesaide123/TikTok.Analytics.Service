using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokOAuthClient
{
    /// <summary>
    /// Builds the TikTok consent URL to send the browser to. The caller is responsible for
    /// having issued <paramref name="state"/> through <see cref="IOAuthStateStore"/> first.
    /// </summary>
    string BuildAuthorizeUrl(string state);

    /// <summary>Trades the one-time authorization code from the callback for a token set.</summary>
    Task<TikTokToken> ExchangeCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Trades a refresh token for a fresh token set. TikTok also rotates the refresh token.</summary>
    Task<TikTokToken> RefreshAsync(string refreshToken, CancellationToken ct = default);
}
