namespace TikTok.Analytics.Domain.Entities;

public class ApiCallLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponsePayload { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
}
