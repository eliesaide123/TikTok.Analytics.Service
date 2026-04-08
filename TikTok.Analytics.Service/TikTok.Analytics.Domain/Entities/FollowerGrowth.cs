namespace TikTok.Analytics.Domain.Entities;

public class FollowerGrowth
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public DateTime MetricDate { get; set; }
    public long FollowersCount { get; set; }
    public long DailyNewFollowers { get; set; }
    public long DailyLostFollowers { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
