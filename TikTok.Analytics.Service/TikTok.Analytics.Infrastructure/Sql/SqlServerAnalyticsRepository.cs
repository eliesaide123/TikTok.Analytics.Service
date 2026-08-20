using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Sql;

/// <summary>
/// Writes ingested data to SQL Server. Schema lives in Docs/sqlserver-schema.sql.
///
/// Like the BigQuery implementation this only ever appends: each run adds rows with a new
/// snapshot_date rather than updating in place, so the history of how a metric moved is kept.
/// </summary>
public class SqlServerAnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerAnalyticsRepository> _logger;

    public SqlServerAnalyticsRepository(
        IOptions<TikTokIngestionOptions> options,
        ILogger<SqlServerAnalyticsRepository> logger)
    {
        var configured = options.Value.SqlServer.ConnectionString;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                "TikTokIngestion:SqlServer:ConnectionString must be set when StorageProvider is SqlServer.");

        _connectionString = configured;
        _logger = logger;
    }

    public Task InsertUserProfileAsync(UserProfile p, CancellationToken ct = default) =>
        ExecuteAsync("tt_user_profiles", 1, """
            INSERT INTO dbo.tt_user_profiles
                (record_id, page_id, open_id, display_name, username, avatar_url, follower_count,
                 following_count, likes_count, video_count, bio_description, is_verified,
                 snapshot_date, ingested_at)
            VALUES
                (@record_id, @page_id, @open_id, @display_name, @username, @avatar_url, @follower_count,
                 @following_count, @likes_count, @video_count, @bio_description, @is_verified,
                 @snapshot_date, @ingested_at);
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("@record_id", p.Id);
                cmd.Parameters.AddWithValue("@page_id", p.PageId);
                cmd.Parameters.AddWithValue("@open_id", Text(p.OpenId));
                cmd.Parameters.AddWithValue("@display_name", Text(p.DisplayName));
                cmd.Parameters.AddWithValue("@username", Text(p.Username));
                cmd.Parameters.AddWithValue("@avatar_url", Text(p.AvatarUrl));
                cmd.Parameters.AddWithValue("@follower_count", p.FollowerCount);
                cmd.Parameters.AddWithValue("@following_count", p.FollowingCount);
                cmd.Parameters.AddWithValue("@likes_count", p.LikesCount);
                cmd.Parameters.AddWithValue("@video_count", p.VideoCount);
                cmd.Parameters.AddWithValue("@bio_description", Text(p.BioDescription));
                cmd.Parameters.AddWithValue("@is_verified", p.IsVerified);
                cmd.Parameters.AddWithValue("@snapshot_date", p.SnapshotDate.Date);
                cmd.Parameters.AddWithValue("@ingested_at", p.IngestedAt);
            }, ct);

    public Task InsertVideosAsync(IEnumerable<Video> videos, CancellationToken ct = default) =>
        ExecuteManyAsync("tt_videos", videos, """
            INSERT INTO dbo.tt_videos
                (video_id, page_id, title, create_time, view_count, like_count, comment_count,
                 share_count, duration, cover_image_url, embed_link, snapshot_date, ingested_at)
            VALUES
                (@video_id, @page_id, @title, @create_time, @view_count, @like_count, @comment_count,
                 @share_count, @duration, @cover_image_url, @embed_link, @snapshot_date, @ingested_at);
            """,
            (cmd, v) =>
            {
                cmd.Parameters.AddWithValue("@video_id", v.Id);
                cmd.Parameters.AddWithValue("@page_id", v.PageId);
                cmd.Parameters.AddWithValue("@title", Text(v.Title));
                cmd.Parameters.AddWithValue("@create_time", v.CreateTime);
                cmd.Parameters.AddWithValue("@view_count", v.ViewCount);
                cmd.Parameters.AddWithValue("@like_count", v.LikeCount);
                cmd.Parameters.AddWithValue("@comment_count", v.CommentCount);
                cmd.Parameters.AddWithValue("@share_count", v.ShareCount);
                cmd.Parameters.AddWithValue("@duration", v.Duration);
                cmd.Parameters.AddWithValue("@cover_image_url", Text(v.CoverImageUrl));
                cmd.Parameters.AddWithValue("@embed_link", Text(v.EmbedLink));
                cmd.Parameters.AddWithValue("@snapshot_date", v.SnapshotDate.Date);
                cmd.Parameters.AddWithValue("@ingested_at", v.IngestedAt);
            }, ct);

    public Task InsertAccountMetricsAsync(AccountMetrics m, CancellationToken ct = default) =>
        ExecuteAsync("tt_account_metrics", 1, """
            INSERT INTO dbo.tt_account_metrics
                (record_id, page_id, business_id, metric_date, impressions, reach, profile_views,
                 video_views, likes, comments, shares, new_followers, followers_count, ingested_at)
            VALUES
                (@record_id, @page_id, @business_id, @metric_date, @impressions, @reach, @profile_views,
                 @video_views, @likes, @comments, @shares, @new_followers, @followers_count, @ingested_at);
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("@record_id", m.Id);
                cmd.Parameters.AddWithValue("@page_id", m.PageId);
                cmd.Parameters.AddWithValue("@business_id", Text(m.BusinessId));
                cmd.Parameters.AddWithValue("@metric_date", m.MetricDate.Date);
                cmd.Parameters.AddWithValue("@impressions", m.Impressions);
                cmd.Parameters.AddWithValue("@reach", m.Reach);
                cmd.Parameters.AddWithValue("@profile_views", m.ProfileViews);
                cmd.Parameters.AddWithValue("@video_views", m.VideoViews);
                cmd.Parameters.AddWithValue("@likes", m.Likes);
                cmd.Parameters.AddWithValue("@comments", m.Comments);
                cmd.Parameters.AddWithValue("@shares", m.Shares);
                cmd.Parameters.AddWithValue("@new_followers", m.NewFollowers);
                cmd.Parameters.AddWithValue("@followers_count", m.FollowersCount);
                cmd.Parameters.AddWithValue("@ingested_at", m.IngestedAt);
            }, ct);

    public Task InsertFollowerGrowthAsync(FollowerGrowth g, CancellationToken ct = default) =>
        ExecuteAsync("tt_follower_growth", 1, """
            INSERT INTO dbo.tt_follower_growth
                (record_id, page_id, business_id, metric_date, followers_count,
                 daily_new_followers, daily_lost_followers, ingested_at)
            VALUES
                (@record_id, @page_id, @business_id, @metric_date, @followers_count,
                 @daily_new_followers, @daily_lost_followers, @ingested_at);
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("@record_id", g.Id);
                cmd.Parameters.AddWithValue("@page_id", g.PageId);
                cmd.Parameters.AddWithValue("@business_id", Text(g.BusinessId));
                cmd.Parameters.AddWithValue("@metric_date", g.MetricDate.Date);
                cmd.Parameters.AddWithValue("@followers_count", g.FollowersCount);
                cmd.Parameters.AddWithValue("@daily_new_followers", g.DailyNewFollowers);
                cmd.Parameters.AddWithValue("@daily_lost_followers", g.DailyLostFollowers);
                cmd.Parameters.AddWithValue("@ingested_at", g.IngestedAt);
            }, ct);

    public Task InsertAudienceDemographicsAsync(IEnumerable<AudienceDemographic> items, CancellationToken ct = default) =>
        ExecuteManyAsync("tt_audience_demographics", items, """
            INSERT INTO dbo.tt_audience_demographics
                (record_id, page_id, business_id, snapshot_date, segment_type, segment_value,
                 percentage, ingested_at)
            VALUES
                (@record_id, @page_id, @business_id, @snapshot_date, @segment_type, @segment_value,
                 @percentage, @ingested_at);
            """,
            (cmd, d) =>
            {
                cmd.Parameters.AddWithValue("@record_id", d.Id);
                cmd.Parameters.AddWithValue("@page_id", d.PageId);
                cmd.Parameters.AddWithValue("@business_id", Text(d.BusinessId));
                cmd.Parameters.AddWithValue("@snapshot_date", d.SnapshotDate.Date);
                cmd.Parameters.AddWithValue("@segment_type", d.SegmentType);
                cmd.Parameters.AddWithValue("@segment_value", Text(d.SegmentValue));
                cmd.Parameters.AddWithValue("@percentage", d.Percentage);
                cmd.Parameters.AddWithValue("@ingested_at", d.IngestedAt);
            }, ct);

    public Task InsertVideoAnalyticsAsync(IEnumerable<VideoAnalytics> items, CancellationToken ct = default) =>
        ExecuteManyAsync("tt_video_analytics", items, """
            INSERT INTO dbo.tt_video_analytics
                (record_id, page_id, video_id, impressions, reach, view_count, likes, comments,
                 shares, saves, average_watch_time, total_watch_time, full_video_watched_rate,
                 video_views_p25, video_views_p50, video_views_p75, video_views_p100,
                 snapshot_date, ingested_at)
            VALUES
                (@record_id, @page_id, @video_id, @impressions, @reach, @view_count, @likes, @comments,
                 @shares, @saves, @average_watch_time, @total_watch_time, @full_video_watched_rate,
                 @p25, @p50, @p75, @p100, @snapshot_date, @ingested_at);
            """,
            (cmd, a) =>
            {
                cmd.Parameters.AddWithValue("@record_id", a.Id);
                cmd.Parameters.AddWithValue("@page_id", a.PageId);
                cmd.Parameters.AddWithValue("@video_id", a.VideoId);
                cmd.Parameters.AddWithValue("@impressions", a.Impressions);
                cmd.Parameters.AddWithValue("@reach", a.Reach);
                cmd.Parameters.AddWithValue("@view_count", a.ViewCount);
                cmd.Parameters.AddWithValue("@likes", a.Likes);
                cmd.Parameters.AddWithValue("@comments", a.Comments);
                cmd.Parameters.AddWithValue("@shares", a.Shares);
                cmd.Parameters.AddWithValue("@saves", a.Saves);
                cmd.Parameters.AddWithValue("@average_watch_time", a.AverageWatchTime);
                cmd.Parameters.AddWithValue("@total_watch_time", a.TotalWatchTime);
                cmd.Parameters.AddWithValue("@full_video_watched_rate", a.FullVideoWatchedRate);
                cmd.Parameters.AddWithValue("@p25", a.VideoViewsP25);
                cmd.Parameters.AddWithValue("@p50", a.VideoViewsP50);
                cmd.Parameters.AddWithValue("@p75", a.VideoViewsP75);
                cmd.Parameters.AddWithValue("@p100", a.VideoViewsP100);
                cmd.Parameters.AddWithValue("@snapshot_date", a.SnapshotDate.Date);
                cmd.Parameters.AddWithValue("@ingested_at", a.IngestedAt);
            }, ct);

    public Task InsertTrafficSourcesAsync(IEnumerable<TrafficSource> items, CancellationToken ct = default) =>
        ExecuteManyAsync("tt_traffic_sources", items, """
            INSERT INTO dbo.tt_traffic_sources
                (record_id, page_id, video_id, source_type, percentage, snapshot_date, ingested_at)
            VALUES
                (@record_id, @page_id, @video_id, @source_type, @percentage, @snapshot_date, @ingested_at);
            """,
            (cmd, s) =>
            {
                cmd.Parameters.AddWithValue("@record_id", s.Id);
                cmd.Parameters.AddWithValue("@page_id", s.PageId);
                cmd.Parameters.AddWithValue("@video_id", s.VideoId);
                cmd.Parameters.AddWithValue("@source_type", Text(s.SourceType));
                cmd.Parameters.AddWithValue("@percentage", s.Percentage);
                cmd.Parameters.AddWithValue("@snapshot_date", s.SnapshotDate.Date);
                cmd.Parameters.AddWithValue("@ingested_at", s.IngestedAt);
            }, ct);

    public Task InsertApiCallLogAsync(ApiCallLog log, CancellationToken ct = default) =>
        ExecuteAsync("tt_api_call_logs", 1, """
            INSERT INTO dbo.tt_api_call_logs
                (record_id, page_id, endpoint, http_method, request_url, request_payload,
                 response_status_code, response_payload, duration_ms, success, error_message, called_at)
            VALUES
                (@record_id, @page_id, @endpoint, @http_method, @request_url, @request_payload,
                 @response_status_code, @response_payload, @duration_ms, @success, @error_message, @called_at);
            """,
            cmd =>
            {
                cmd.Parameters.AddWithValue("@record_id", log.Id);
                cmd.Parameters.AddWithValue("@page_id", Text(log.PageId));
                cmd.Parameters.AddWithValue("@endpoint", Text(log.Endpoint));
                cmd.Parameters.AddWithValue("@http_method", Text(log.HttpMethod));
                cmd.Parameters.AddWithValue("@request_url", Text(log.RequestUrl));
                cmd.Parameters.AddWithValue("@request_payload", Text(log.RequestPayload));
                cmd.Parameters.AddWithValue("@response_status_code", log.ResponseStatusCode);
                // Matches the BigQuery implementation's cap so one oversized body cannot bloat a row.
                cmd.Parameters.AddWithValue("@response_payload", Text(Truncate(log.ResponsePayload, 65536)));
                cmd.Parameters.AddWithValue("@duration_ms", log.DurationMs);
                cmd.Parameters.AddWithValue("@success", log.Success);
                cmd.Parameters.AddWithValue("@error_message", Text(Truncate(log.ErrorMessage, 8192)));
                cmd.Parameters.AddWithValue("@called_at", log.CalledAt);
            }, ct);

    // ------------------------------------------------------------------ helpers --

    private async Task ExecuteAsync(string table, int rowCount, string sql, Action<SqlCommand> bind, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, connection);
            bind(cmd);
            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogInformation("Inserted {Count} row(s) into {Table}", rowCount, table);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed inserting into {Table}", table);
            throw;
        }
    }

    /// <summary>
    /// Inserts a batch inside one transaction on a single connection, re-binding parameters
    /// per row. All-or-nothing: a partial batch would leave a snapshot that looks complete.
    /// </summary>
    private async Task ExecuteManyAsync<T>(string table, IEnumerable<T> items, string sql, Action<SqlCommand, T> bind, CancellationToken ct)
    {
        var rows = items as IList<T> ?? items.ToList();
        if (rows.Count == 0)
            return;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

            await using var cmd = new SqlCommand(sql, connection, transaction);
            foreach (var row in rows)
            {
                cmd.Parameters.Clear();
                bind(cmd, row);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogInformation("Inserted {Count} row(s) into {Table}", rows.Count, table);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed inserting {Count} row(s) into {Table}", rows.Count, table);
            throw;
        }
    }

    // The domain uses empty strings rather than nulls; store those as NULL.
    private static object Text(string? value) =>
        string.IsNullOrEmpty(value) ? DBNull.Value : value;

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty :
        value.Length <= max ? value : value[..max];
}
