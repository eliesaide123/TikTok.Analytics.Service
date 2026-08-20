using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokBusinessOAuthClient
{
    /// <summary>
    /// Consent URL for the TikTok for Business app. The caller must have issued
    /// <paramref name="state"/> through <see cref="IOAuthStateStore"/> first.
    /// </summary>
    string BuildAuthorizeUrl(string state);

    /// <summary>Trades the one-time auth code from the callback for a business token.</summary>
    Task<BusinessToken> ExchangeCodeAsync(string authCode, CancellationToken ct = default);
}
