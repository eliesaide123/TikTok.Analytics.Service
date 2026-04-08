namespace TikTok.Analytics.Domain.Entities;

public class Video
{
    public string Id { get; set; } = string.Empty;
    public string PageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public long ViewCount { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public long ShareCount { get; set; }
    public int Duration { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public string EmbedLink { get; set; } = string.Empty;
    public DateTime SnapshotDate { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
