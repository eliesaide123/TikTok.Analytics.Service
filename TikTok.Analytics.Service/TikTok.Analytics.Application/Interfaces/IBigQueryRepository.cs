using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface IBigQueryRepository
{
    Task InsertUserProfileAsync(UserProfile profile, CancellationToken ct = default);
    Task InsertVideosAsync(IEnumerable<Video> videos, CancellationToken ct = default);
    Task InsertAccountMetricsAsync(AccountMetrics metrics, CancellationToken ct = default);
    Task InsertFollowerGrowthAsync(FollowerGrowth growth, CancellationToken ct = default);
    Task InsertAudienceDemographicsAsync(IEnumerable<AudienceDemographic> demographics, CancellationToken ct = default);
    Task InsertVideoAnalyticsAsync(IEnumerable<VideoAnalytics> analytics, CancellationToken ct = default);
    Task InsertTrafficSourcesAsync(IEnumerable<TrafficSource> sources, CancellationToken ct = default);
    Task InsertApiCallLogAsync(ApiCallLog log, CancellationToken ct = default);
}
