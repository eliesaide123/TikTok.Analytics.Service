using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Api;

public abstract class TikTokApiClientBase
{
    protected readonly HttpClient HttpClient;
    private readonly IApiCallLogger _callLogger;
    private readonly ILogger _logger;

    protected TikTokApiClientBase(HttpClient httpClient, IApiCallLogger callLogger, ILogger logger)
    {
        HttpClient = httpClient;
        _callLogger = callLogger;
        _logger = logger;
    }

    protected async Task<T> SendAsync<T>(string pageId, string endpoint, string httpMethod, string url, HttpContent? content = null, CancellationToken ct = default)
    {
        var requestPayload = content != null ? await content.ReadAsStringAsync(ct) : string.Empty;
        _logger.LogInformation("TikTok API call: {Method} {Endpoint} for page {PageId}", httpMethod, endpoint, pageId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), url);
            if (content != null)
                request.Content = content;

            var response = await HttpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            stopwatch.Stop();

            var success = response.IsSuccessStatusCode;
            _logger.LogInformation("TikTok API response: {StatusCode} in {Duration}ms for {Endpoint}", (int)response.StatusCode, stopwatch.ElapsedMilliseconds, endpoint);

            await _callLogger.LogCallAsync(pageId, endpoint, httpMethod, url, requestPayload, (int)response.StatusCode, responseBody, stopwatch.ElapsedMilliseconds, success, success ? "" : responseBody, ct);

            response.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<T>(responseBody) ?? throw new JsonException($"Failed to deserialize response from {endpoint}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "TikTok API error: {Endpoint} for page {PageId} after {Duration}ms", endpoint, pageId, stopwatch.ElapsedMilliseconds);

            if (ex is not HttpRequestException)
            {
                await _callLogger.LogCallAsync(pageId, endpoint, httpMethod, url, requestPayload, 0, string.Empty, stopwatch.ElapsedMilliseconds, false, ex.Message, ct);
            }
            throw;
        }
    }
}
