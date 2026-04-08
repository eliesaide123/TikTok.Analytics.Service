namespace TikTok.Analytics.Domain.Entities;

public class VideoAnalytics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public long Impressions { get; set; }
    public long Reach { get; set; }
    public long ViewCount { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }
    public long Shares { get; set; }
    public long Saves { get; set; }
    public double AverageWatchTime { get; set; }
    public double TotalWatchTime { get; set; }
    public double FullVideoWatchedRate { get; set; }
    public long VideoViewsP25 { get; set; }
    public long VideoViewsP50 { get; set; }
    public long VideoViewsP75 { get; set; }
    public long VideoViewsP100 { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
