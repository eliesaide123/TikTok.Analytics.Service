using TikTok.Analytics.Application.DTOs.Business;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokBusinessApiClient
{
    Task<BusinessApiResponse<AccountMetricsDto>> GetAccountMetricsAsync(string accessToken, string businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<BusinessApiResponse<FollowerGrowthDto>> GetFollowerGrowthAsync(string accessToken, string businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<BusinessApiResponse<AudienceDemographicsDto>> GetAudienceDemographicsAsync(string accessToken, string businessId, CancellationToken ct = default);
    Task<BusinessApiResponse<BusinessVideoListData>> GetVideoAnalyticsAsync(string accessToken, string businessId, long cursor = 0, CancellationToken ct = default);
}
