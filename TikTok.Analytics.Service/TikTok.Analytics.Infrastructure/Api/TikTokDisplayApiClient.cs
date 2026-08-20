using Microsoft.Extensions.Logging;
using TikTok.Analytics.Application.DTOs.Display;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Api;

public class TikTokDisplayApiClient : TikTokApiClientBase, ITikTokDisplayApiClient
{
    private const string BaseUrl = "https://open.tiktokapis.com/v2";

    public TikTokDisplayApiClient(HttpClient httpClient, IApiCallLogger callLogger, ILogger<TikTokDisplayApiClient> logger)
        : base(httpClient, callLogger, logger) { }

    public async Task<DisplayApiResponse<UserInfoData>> GetUserInfoAsync(string accessToken, CancellationToken ct = default)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var url = $"{BaseUrl}/user/info/?fields=open_id,display_name,username,avatar_url,follower_count,following_count,likes_count,video_count,bio_description,is_verified";
        return await SendAsync<DisplayApiResponse<UserInfoData>>("", "user/info", "GET", url, ct: ct);
    }

    public async Task<DisplayApiResponse<VideoListData>> GetVideoListAsync(string accessToken, long cursor = 0, int maxCount = 20, CancellationToken ct = default)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // video/list is POST, unlike user/info. Calling it with GET returns
        // "Unsupported path(Janus)". The requested fields stay in the query string;
        // the paging arguments go in the JSON body.
        var url = $"{BaseUrl}/video/list/?fields=id,title,create_time,like_count,comment_count,share_count,view_count,duration,cover_image_url,embed_link";

        // Only send a cursor once we have one. A literal 0 is read as an epoch
        // timestamp and returns nothing.
        var body = cursor > 0
            ? $"{{\"max_count\":{maxCount},\"cursor\":{cursor}}}"
            : $"{{\"max_count\":{maxCount}}}";

        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        return await SendAsync<DisplayApiResponse<VideoListData>>("", "video/list", "POST", url, content, ct);
    }
}
