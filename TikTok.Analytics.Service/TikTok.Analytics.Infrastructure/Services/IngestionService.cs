using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.DTOs.Business;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Services;

public class IngestionService : IIngestionService
{
    private readonly ITikTokDisplayApiClient _displayClient;
    private readonly ITikTokBusinessApiClient _businessClient;
    private readonly IBigQueryRepository _repository;
    private readonly IMapper _mapper;
    private readonly TikTokIngestionOptions _options;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        ITikTokDisplayApiClient displayClient,
        ITikTokBusinessApiClient businessClient,
        IBigQueryRepository repository,
        IMapper mapper,
        IOptions<TikTokIngestionOptions> options,
        ILogger<IngestionService> logger)
    {
        _displayClient = displayClient;
        _businessClient = businessClient;
        _repository = repository;
        _mapper = mapper;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunIngestionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting TikTok data ingestion. Pages configured: {Count}", _options.Pages.Count);

        var today = DateTime.UtcNow.Date;
        var lookbackDate = today.AddDays(-_options.LookbackDays);
        var effectiveStartDate = lookbackDate > _options.HistoryStartDate ? lookbackDate : _options.HistoryStartDate;

        _logger.LogInformation("Ingestion window: {Start} to {End} (lookback {Days} days, history cutoff {History})",
            effectiveStartDate, today, _options.LookbackDays, _options.HistoryStartDate);

        foreach (var page in _options.Pages.Where(p => p.Enabled))
        {
            _logger.LogInformation("Processing page: {PageName} ({PageId})", page.PageName, page.PageId);

            try
            {
                await IngestUserProfileAsync(page, ct);
                await IngestVideosAsync(page, effectiveStartDate, ct);
                await IngestAccountMetricsAsync(page, effectiveStartDate, today, ct);
                await IngestFollowerGrowthAsync(page, effectiveStartDate, today, ct);
                await IngestDemographicsAsync(page, ct);
                await IngestVideoAnalyticsAsync(page, ct);

                _logger.LogInformation("Completed ingestion for page: {PageName}", page.PageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed ingestion for page: {PageName} ({PageId}). Continuing to next page.", page.PageName, page.PageId);
            }
        }

        _logger.LogInformation("TikTok data ingestion completed");
    }

    private async Task IngestUserProfileAsync(PageConfig page, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting user profile for {PageName}", page.PageName);
        var response = await _displayClient.GetUserInfoAsync(page.DisplayAccessToken, ct);

        if (response.Data?.User == null)
        {
            _logger.LogWarning("No user data returned for page {PageId}", page.PageId);
            return;
        }

        var profile = _mapper.Map<UserProfile>(response.Data.User);
        profile.PageId = page.PageId;
        await _repository.InsertUserProfileAsync(profile, ct);
    }

    private async Task IngestVideosAsync(PageConfig page, DateTime effectiveStartDate, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting videos for {PageName} since {Since}", page.PageName, effectiveStartDate);

        long cursor = 0;
        bool hasMore = true;
        var allVideos = new List<Video>();

        while (hasMore)
        {
            var response = await _displayClient.GetVideoListAsync(page.DisplayAccessToken, cursor, 20, ct);

            if (response.Data?.Videos == null || response.Data.Videos.Count == 0)
                break;

            foreach (var videoDto in response.Data.Videos)
            {
                var video = _mapper.Map<Video>(videoDto);
                video.PageId = page.PageId;

                if (video.CreateTime >= effectiveStartDate)
                    allVideos.Add(video);
            }

            hasMore = response.Data.HasMore;
            cursor = response.Data.Cursor;

            // If the oldest video in this batch is before our window, stop paginating
            var oldestInBatch = response.Data.Videos.Min(v => v.CreateTime);
            if (DateTimeOffset.FromUnixTimeSeconds(oldestInBatch).UtcDateTime < effectiveStartDate)
                break;
        }

        if (allVideos.Count > 0)
        {
            await _repository.InsertVideosAsync(allVideos, ct);
            _logger.LogInformation("Ingested {Count} videos for {PageName}", allVideos.Count, page.PageName);
        }
    }

    private async Task IngestAccountMetricsAsync(PageConfig page, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting account metrics for {PageName}", page.PageName);
        var response = await _businessClient.GetAccountMetricsAsync(page.BusinessAccessToken, page.BusinessId, startDate, endDate, ct);

        if (response.Data == null)
        {
            _logger.LogWarning("No account metrics returned for page {PageId}", page.PageId);
            return;
        }

        var metrics = _mapper.Map<AccountMetrics>(response.Data);
        metrics.PageId = page.PageId;
        metrics.BusinessId = page.BusinessId;
        metrics.MetricDate = endDate;
        await _repository.InsertAccountMetricsAsync(metrics, ct);
    }

    private async Task IngestFollowerGrowthAsync(PageConfig page, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting follower growth for {PageName}", page.PageName);
        var response = await _businessClient.GetFollowerGrowthAsync(page.BusinessAccessToken, page.BusinessId, startDate, endDate, ct);

        if (response.Data == null)
        {
            _logger.LogWarning("No follower growth data returned for page {PageId}", page.PageId);
            return;
        }

        var growth = _mapper.Map<FollowerGrowth>(response.Data);
        growth.PageId = page.PageId;
        growth.BusinessId = page.BusinessId;
        growth.MetricDate = endDate;
        await _repository.InsertFollowerGrowthAsync(growth, ct);
    }

    private async Task IngestDemographicsAsync(PageConfig page, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting demographics for {PageName}", page.PageName);
        var response = await _businessClient.GetAudienceDemographicsAsync(page.BusinessAccessToken, page.BusinessId, ct);

        if (response.Data == null)
        {
            _logger.LogWarning("No demographics data returned for page {PageId}", page.PageId);
            return;
        }

        var demographics = new List<AudienceDemographic>();
        var today = DateTime.UtcNow.Date;

        foreach (var g in response.Data.AudienceGenders)
            demographics.Add(new AudienceDemographic { PageId = page.PageId, BusinessId = page.BusinessId, SnapshotDate = today, SegmentType = "gender", SegmentValue = g.Name, Percentage = g.Percentage });

        foreach (var a in response.Data.AudienceAges)
            demographics.Add(new AudienceDemographic { PageId = page.PageId, BusinessId = page.BusinessId, SnapshotDate = today, SegmentType = "age", SegmentValue = a.Name, Percentage = a.Percentage });

        foreach (var c in response.Data.AudienceCountries)
            demographics.Add(new AudienceDemographic { PageId = page.PageId, BusinessId = page.BusinessId, SnapshotDate = today, SegmentType = "country", SegmentValue = c.Name, Percentage = c.Percentage });

        if (demographics.Count > 0)
            await _repository.InsertAudienceDemographicsAsync(demographics, ct);
    }

    private async Task IngestVideoAnalyticsAsync(PageConfig page, CancellationToken ct)
    {
        _logger.LogInformation("Ingesting video analytics for {PageName}", page.PageName);

        long cursor = 0;
        bool hasMore = true;
        var allAnalytics = new List<VideoAnalytics>();
        var allTrafficSources = new List<TrafficSource>();
        var today = DateTime.UtcNow.Date;

        while (hasMore)
        {
            var response = await _businessClient.GetVideoAnalyticsAsync(page.BusinessAccessToken, page.BusinessId, cursor, ct);

            if (response.Data?.Videos == null || response.Data.Videos.Count == 0)
                break;

            foreach (var bizVideo in response.Data.Videos)
            {
                var analytics = _mapper.Map<VideoAnalytics>(bizVideo);
                analytics.PageId = page.PageId;
                allAnalytics.Add(analytics);

                // Extract traffic sources
                foreach (var (sourceType, percentage) in bizVideo.TrafficSourceTypes)
                {
                    allTrafficSources.Add(new TrafficSource
                    {
                        PageId = page.PageId,
                        VideoId = bizVideo.ItemId,
                        SourceType = sourceType,
                        Percentage = percentage,
                        SnapshotDate = today
                    });
                }
            }

            hasMore = response.Data.HasMore;
            cursor = response.Data.Cursor;
        }

        if (allAnalytics.Count > 0)
            await _repository.InsertVideoAnalyticsAsync(allAnalytics, ct);

        if (allTrafficSources.Count > 0)
            await _repository.InsertTrafficSourcesAsync(allTrafficSources, ct);

        _logger.LogInformation("Ingested analytics for {Count} videos for {PageName}", allAnalytics.Count, page.PageName);
    }
}
