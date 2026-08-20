using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface IBusinessTokenStore
{
    Task<BusinessToken?> GetByPageAsync(string pageId, CancellationToken ct = default);
    Task<IReadOnlyList<BusinessToken>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Inserts or replaces the credential for a BusinessId.</summary>
    Task SaveAsync(BusinessToken token, CancellationToken ct = default);

    Task DeleteAsync(string businessId, CancellationToken ct = default);
}
