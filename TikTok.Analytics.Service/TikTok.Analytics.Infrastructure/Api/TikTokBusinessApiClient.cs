using Microsoft.Extensions.Logging;
using TikTok.Analytics.Application.DTOs.Business;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Api;

public class TikTokBusinessApiClient : TikTokApiClientBase, ITikTokBusinessApiClient
{
    private const string BaseUrl = "https://business-api.tiktok.com/open_api/v1.3";

    public TikTokBusinessApiClient(HttpClient httpClient, IApiCallLogger callLogger, ILogger<TikTokBusinessApiClient> logger)
        : base(httpClient, callLogger, logger) { }

    private void SetAccessToken(string accessToken)
    {
        HttpClient.DefaultRequestHeaders.Remove("Access-Token");
        HttpClient.DefaultRequestHeaders.Add("Access-Token", accessToken);
    }

    public async Task<BusinessApiResponse<AccountMetricsDto>> GetAccountMetricsAsync(string accessToken, string businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        SetAccessToken(accessToken);
        var fields = Uri.EscapeDataString("[\"impressions\",\"reach\",\"profile_views\",\"video_views\",\"likes\",\"comments\",\"shares\",\"new_followers\",\"followers_count\"]");
        var url = $"{BaseUrl}/business/get/?business_id={businessId}&fields={fields}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await SendAsync<BusinessApiResponse<AccountMetricsDto>>("", "business/get/metrics", "GET", url, ct: ct);
    }

    public async Task<BusinessApiResponse<FollowerGrowthDto>> GetFollowerGrowthAsync(string accessToken, string businessId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        SetAccessToken(accessToken);
        var fields = Uri.EscapeDataString("[\"daily_new_followers\",\"daily_lost_followers\",\"followers_count\"]");
        var url = $"{BaseUrl}/business/get/?business_id={businessId}&fields={fields}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
        return await SendAsync<BusinessApiResponse<FollowerGrowthDto>>("", "business/get/followers", "GET", url, ct: ct);
    }

    public async Task<BusinessApiResponse<AudienceDemographicsDto>> GetAudienceDemographicsAsync(string accessToken, string businessId, CancellationToken ct = default)
    {
        SetAccessToken(accessToken);
        var fields = Uri.EscapeDataString("[\"audience_genders\",\"audience_ages\",\"audience_countries\"]");
        var url = $"{BaseUrl}/business/get/?business_id={businessId}&fields={fields}";
        return await SendAsync<BusinessApiResponse<AudienceDemographicsDto>>("", "business/get/demographics", "GET", url, ct: ct);
    }

    public async Task<BusinessApiResponse<BusinessVideoListData>> GetVideoAnalyticsAsync(string accessToken, string businessId, long cursor = 0, CancellationToken ct = default)
    {
        SetAccessToken(accessToken);
        var fields = Uri.EscapeDataString("[\"item_id\",\"impressions\",\"reach\",\"view_count\",\"likes\",\"comments\",\"shares\",\"saves\",\"average_watch_time\",\"total_watch_time\",\"full_video_watched_rate\",\"video_views_p25\",\"video_views_p50\",\"video_views_p75\",\"video_views_p100\",\"traffic_source_types\"]");
        var url = $"{BaseUrl}/business/video/list/?business_id={businessId}&fields={fields}&cursor={cursor}";
        return await SendAsync<BusinessApiResponse<BusinessVideoListData>>("", "business/video/list", "GET", url, ct: ct);
    }
}
