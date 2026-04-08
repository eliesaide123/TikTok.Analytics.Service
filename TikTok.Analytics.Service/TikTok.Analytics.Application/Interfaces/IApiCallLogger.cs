using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Interfaces;

public interface IApiCallLogger
{
    Task<ApiCallLog> LogCallAsync(string pageId, string endpoint, string httpMethod, string requestUrl, string requestPayload, int responseStatusCode, string responsePayload, long durationMs, bool success, string errorMessage = "", CancellationToken ct = default);
}
