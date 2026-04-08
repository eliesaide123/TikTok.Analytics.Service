using System.Text.Json.Serialization;

namespace TikTok.Analytics.Application.DTOs.Display;

// Base wrapper for all Display API responses
public class DisplayApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public DisplayApiError Error { get; set; } = new();
}

public class DisplayApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("log_id")]
    public string LogId { get; set; } = string.Empty;
}

// User Info
public class UserInfoData
{
    [JsonPropertyName("user")]
    public UserInfoDto User { get; set; } = new();
}

public class UserInfoDto
{
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    [JsonPropertyName("follower_count")]
    public long FollowerCount { get; set; }

    [JsonPropertyName("following_count")]
    public long FollowingCount { get; set; }

    [JsonPropertyName("likes_count")]
    public long LikesCount { get; set; }

    [JsonPropertyName("video_count")]
    public long VideoCount { get; set; }

    [JsonPropertyName("bio_description")]
    public string BioDescription { get; set; } = string.Empty;

    [JsonPropertyName("is_verified")]
    public bool IsVerified { get; set; }
}

// Video List
public class VideoListData
{
    [JsonPropertyName("videos")]
    public List<VideoDto> Videos { get; set; } = new();

    [JsonPropertyName("cursor")]
    public long Cursor { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

public class VideoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("create_time")]
    public long CreateTime { get; set; }

    [JsonPropertyName("view_count")]
    public long ViewCount { get; set; }

    [JsonPropertyName("like_count")]
    public long LikeCount { get; set; }

    [JsonPropertyName("comment_count")]
    public long CommentCount { get; set; }

    [JsonPropertyName("share_count")]
    public long ShareCount { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("cover_image_url")]
    public string CoverImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("embed_link")]
    public string EmbedLink { get; set; } = string.Empty;
}
