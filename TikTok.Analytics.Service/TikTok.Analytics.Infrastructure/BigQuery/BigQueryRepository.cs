using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.BigQuery;

public class BigQueryRepository : IBigQueryRepository
{
    private readonly BigQueryClient _client;
    private readonly string _datasetId;
    private readonly ILogger<BigQueryRepository> _logger;

    public BigQueryRepository(IOptions<TikTokIngestionOptions> options, ILogger<BigQueryRepository> logger)
    {
        _logger = logger;
        _datasetId = options.Value.BigQuery.DatasetId;
        _client = BigQueryClient.Create(options.Value.BigQuery.ProjectId);
    }

    public async Task InsertUserProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        var row = new BigQueryInsertRow
        {
            { "id", profile.Id },
            { "page_id", profile.PageId },
            { "open_id", profile.OpenId },
            { "display_name", profile.DisplayName },
            { "username", profile.Username },
            { "avatar_url", profile.AvatarUrl },
            { "follower_count", profile.FollowerCount },
            { "following_count", profile.FollowingCount },
            { "likes_count", profile.LikesCount },
            { "video_count", profile.VideoCount },
            { "bio_description", profile.BioDescription },
            { "is_verified", profile.IsVerified },
            { "snapshot_date", profile.SnapshotDate.ToString("yyyy-MM-dd") },
            { "ingested_at", profile.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        };
        await InsertRowAsync("user_profiles", row);
    }

    public async Task InsertVideosAsync(IEnumerable<Video> videos, CancellationToken ct = default)
    {
        var rows = videos.Select(v => new BigQueryInsertRow
        {
            { "id", v.Id },
            { "page_id", v.PageId },
            { "title", v.Title },
            { "create_time", v.CreateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") },
            { "view_count", v.ViewCount },
            { "like_count", v.LikeCount },
            { "comment_count", v.CommentCount },
            { "share_count", v.ShareCount },
            { "duration", v.Duration },
            { "cover_image_url", v.CoverImageUrl },
            { "embed_link", v.EmbedLink },
            { "snapshot_date", v.SnapshotDate.ToString("yyyy-MM-dd") },
            { "ingested_at", v.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        }).ToList();

        if (rows.Count > 0)
            await InsertRowsAsync("videos", rows);
    }

    public async Task InsertAccountMetricsAsync(AccountMetrics metrics, CancellationToken ct = default)
    {
        var row = new BigQueryInsertRow
        {
            { "id", metrics.Id },
            { "page_id", metrics.PageId },
            { "business_id", metrics.BusinessId },
            { "metric_date", metrics.MetricDate.ToString("yyyy-MM-dd") },
            { "impressions", metrics.Impressions },
            { "reach", metrics.Reach },
            { "profile_views", metrics.ProfileViews },
            { "video_views", metrics.VideoViews },
            { "likes", metrics.Likes },
            { "comments", metrics.Comments },
            { "shares", metrics.Shares },
            { "new_followers", metrics.NewFollowers },
            { "followers_count", metrics.FollowersCount },
            { "ingested_at", metrics.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        };
        await InsertRowAsync("account_metrics", row);
    }

    public async Task InsertFollowerGrowthAsync(FollowerGrowth growth, CancellationToken ct = default)
    {
        var row = new BigQueryInsertRow
        {
            { "id", growth.Id },
            { "page_id", growth.PageId },
            { "business_id", growth.BusinessId },
            { "metric_date", growth.MetricDate.ToString("yyyy-MM-dd") },
            { "followers_count", growth.FollowersCount },
            { "daily_new_followers", growth.DailyNewFollowers },
            { "daily_lost_followers", growth.DailyLostFollowers },
            { "ingested_at", growth.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        };
        await InsertRowAsync("follower_growth", row);
    }

    public async Task InsertAudienceDemographicsAsync(IEnumerable<AudienceDemographic> demographics, CancellationToken ct = default)
    {
        var rows = demographics.Select(d => new BigQueryInsertRow
        {
            { "id", d.Id },
            { "page_id", d.PageId },
            { "business_id", d.BusinessId },
            { "snapshot_date", d.SnapshotDate.ToString("yyyy-MM-dd") },
            { "segment_type", d.SegmentType },
            { "segment_value", d.SegmentValue },
            { "percentage", d.Percentage },
            { "ingested_at", d.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        }).ToList();

        if (rows.Count > 0)
            await InsertRowsAsync("audience_demographics", rows);
    }

    public async Task InsertVideoAnalyticsAsync(IEnumerable<VideoAnalytics> analytics, CancellationToken ct = default)
    {
        var rows = analytics.Select(a => new BigQueryInsertRow
        {
            { "id", a.Id },
            { "page_id", a.PageId },
            { "video_id", a.VideoId },
            { "impressions", a.Impressions },
            { "reach", a.Reach },
            { "view_count", a.ViewCount },
            { "likes", a.Likes },
            { "comments", a.Comments },
            { "shares", a.Shares },
            { "saves", a.Saves },
            { "average_watch_time", a.AverageWatchTime },
            { "total_watch_time", a.TotalWatchTime },
            { "full_video_watched_rate", a.FullVideoWatchedRate },
            { "video_views_p25", a.VideoViewsP25 },
            { "video_views_p50", a.VideoViewsP50 },
            { "video_views_p75", a.VideoViewsP75 },
            { "video_views_p100", a.VideoViewsP100 },
            { "snapshot_date", a.SnapshotDate.ToString("yyyy-MM-dd") },
            { "ingested_at", a.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        }).ToList();

        if (rows.Count > 0)
            await InsertRowsAsync("video_analytics", rows);
    }

    public async Task InsertTrafficSourcesAsync(IEnumerable<TrafficSource> sources, CancellationToken ct = default)
    {
        var rows = sources.Select(s => new BigQueryInsertRow
        {
            { "id", s.Id },
            { "page_id", s.PageId },
            { "video_id", s.VideoId },
            { "source_type", s.SourceType },
            { "percentage", s.Percentage },
            { "snapshot_date", s.SnapshotDate.ToString("yyyy-MM-dd") },
            { "ingested_at", s.IngestedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        }).ToList();

        if (rows.Count > 0)
            await InsertRowsAsync("traffic_sources", rows);
    }

    public async Task InsertApiCallLogAsync(ApiCallLog log, CancellationToken ct = default)
    {
        var row = new BigQueryInsertRow
        {
            { "id", log.Id },
            { "page_id", log.PageId },
            { "endpoint", log.Endpoint },
            { "http_method", log.HttpMethod },
            { "request_url", log.RequestUrl },
            { "request_payload", log.RequestPayload },
            { "response_status_code", log.ResponseStatusCode },
            { "response_payload", log.ResponsePayload },
            { "duration_ms", log.DurationMs },
            { "success", log.Success },
            { "error_message", log.ErrorMessage },
            { "called_at", log.CalledAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") }
        };
        await InsertRowAsync("api_call_logs", row);
    }

    private async Task InsertRowAsync(string tableId, BigQueryInsertRow row)
    {
        _logger.LogDebug("Inserting row into {Table}", tableId);
        await _client.InsertRowAsync(_datasetId, tableId, row);
    }

    private async Task InsertRowsAsync(string tableId, IList<BigQueryInsertRow> rows)
    {
        _logger.LogDebug("Inserting {Count} rows into {Table}", rows.Count, tableId);
        await _client.InsertRowsAsync(_datasetId, tableId, rows);
    }
}
