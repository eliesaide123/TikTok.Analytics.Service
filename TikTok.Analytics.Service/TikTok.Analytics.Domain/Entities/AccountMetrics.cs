namespace TikTok.Analytics.Domain.Entities;

public class AccountMetrics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public DateTime MetricDate { get; set; }
    public long Impressions { get; set; }
    public long Reach { get; set; }
    public long ProfileViews { get; set; }
    public long VideoViews { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }
    public long Shares { get; set; }
    public long NewFollowers { get; set; }
    public long FollowersCount { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
