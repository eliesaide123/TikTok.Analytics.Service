using TikTok.Analytics.Application.DTOs.Display;

namespace TikTok.Analytics.Application.Interfaces;

public interface ITikTokDisplayApiClient
{
    Task<DisplayApiResponse<UserInfoData>> GetUserInfoAsync(string accessToken, CancellationToken ct = default);
    Task<DisplayApiResponse<VideoListData>> GetVideoListAsync(string accessToken, long cursor = 0, int maxCount = 20, CancellationToken ct = default);
}
