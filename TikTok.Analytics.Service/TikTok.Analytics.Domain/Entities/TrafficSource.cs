namespace TikTok.Analytics.Domain.Entities;

public class TrafficSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
