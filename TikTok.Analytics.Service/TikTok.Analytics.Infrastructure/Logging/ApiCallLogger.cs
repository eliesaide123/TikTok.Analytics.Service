using Microsoft.Extensions.Logging;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Logging;

public class ApiCallLogger : IApiCallLogger
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<ApiCallLogger> _logger;

    public ApiCallLogger(IAnalyticsRepository repository, ILogger<ApiCallLogger> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiCallLog> LogCallAsync(string pageId, string endpoint, string httpMethod, string requestUrl, string requestPayload, int responseStatusCode, string responsePayload, long durationMs, bool success, string errorMessage = "", CancellationToken ct = default)
    {
        var log = new ApiCallLog
        {
            PageId = pageId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            RequestUrl = requestUrl,
            RequestPayload = requestPayload,
            ResponseStatusCode = responseStatusCode,
            ResponsePayload = TruncatePayload(responsePayload, 64000),
            DurationMs = durationMs,
            Success = success,
            ErrorMessage = errorMessage,
            CalledAt = DateTime.UtcNow
        };

        try
        {
            await _repository.InsertApiCallLogAsync(log, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist API call log for {Endpoint}", endpoint);
        }

        return log;
    }

    private static string TruncatePayload(string payload, int maxLength)
    {
        if (string.IsNullOrEmpty(payload) || payload.Length <= maxLength)
            return payload;
        return payload[..maxLength] + "...[TRUNCATED]";
    }
}
