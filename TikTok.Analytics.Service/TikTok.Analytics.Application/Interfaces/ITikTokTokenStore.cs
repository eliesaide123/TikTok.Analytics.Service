using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokTokenStore
{
    Task<TikTokToken?> GetAsync(string openId, CancellationToken ct = default);

    Task<IReadOnlyList<TikTokToken>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Inserts or replaces the token set for an OpenId.</summary>
    Task SaveAsync(TikTokToken token, CancellationToken ct = default);

    Task DeleteAsync(string openId, CancellationToken ct = default);
}
