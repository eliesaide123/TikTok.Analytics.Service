namespace TikTok.Analytics.Domain.Entities;

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PageId { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public long FollowerCount { get; set; }
    public long FollowingCount { get; set; }
    public long LikesCount { get; set; }
    public long VideoCount { get; set; }
    public string BioDescription { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
