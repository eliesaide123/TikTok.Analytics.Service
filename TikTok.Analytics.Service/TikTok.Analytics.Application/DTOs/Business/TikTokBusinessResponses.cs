using System.Text.Json.Serialization;

namespace TikTok.Analytics.Application.DTOs.Business;

// Base wrapper for all Business API responses
public class BusinessApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

// Account Metrics
public class AccountMetricsDto
{
    [JsonPropertyName("impressions")]
    public long Impressions { get; set; }

    [JsonPropertyName("reach")]
    public long Reach { get; set; }

    [JsonPropertyName("profile_views")]
    public long ProfileViews { get; set; }

    [JsonPropertyName("video_views")]
    public long VideoViews { get; set; }

    [JsonPropertyName("likes")]
    public long Likes { get; set; }

    [JsonPropertyName("comments")]
    public long Comments { get; set; }

    [JsonPropertyName("shares")]
    public long Shares { get; set; }

    [JsonPropertyName("new_followers")]
    public long NewFollowers { get; set; }

    [JsonPropertyName("followers_count")]
    public long FollowersCount { get; set; }
}

// Follower Growth
public class FollowerGrowthDto
{
    [JsonPropertyName("followers_count")]
    public long FollowersCount { get; set; }

    [JsonPropertyName("daily_new_followers")]
    public long DailyNewFollowers { get; set; }

    [JsonPropertyName("daily_lost_followers")]
    public long DailyLostFollowers { get; set; }
}

// Audience Demographics
public class AudienceDemographicsDto
{
    [JsonPropertyName("audience_genders")]
    public List<DemographicItemDto> AudienceGenders { get; set; } = new();

    [JsonPropertyName("audience_ages")]
    public List<DemographicItemDto> AudienceAges { get; set; } = new();

    [JsonPropertyName("audience_countries")]
    public List<DemographicItemDto> AudienceCountries { get; set; } = new();
}

public class DemographicItemDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }
}

// Video Analytics (Business Video List)
public class BusinessVideoListData
{
    [JsonPropertyName("videos")]
    public List<BusinessVideoDto> Videos { get; set; } = new();

    [JsonPropertyName("cursor")]
    public long Cursor { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

public class BusinessVideoDto
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("impressions")]
    public long Impressions { get; set; }

    [JsonPropertyName("reach")]
    public long Reach { get; set; }

    [JsonPropertyName("view_count")]
    public long ViewCount { get; set; }

    [JsonPropertyName("likes")]
    public long Likes { get; set; }

    [JsonPropertyName("comments")]
    public long Comments { get; set; }

    [JsonPropertyName("shares")]
    public long Shares { get; set; }

    [JsonPropertyName("saves")]
    public long Saves { get; set; }

    [JsonPropertyName("average_watch_time")]
    public double AverageWatchTime { get; set; }

    [JsonPropertyName("total_watch_time")]
    public double TotalWatchTime { get; set; }

    [JsonPropertyName("full_video_watched_rate")]
    public double FullVideoWatchedRate { get; set; }

    [JsonPropertyName("video_views_p25")]
    public long VideoViewsP25 { get; set; }

    [JsonPropertyName("video_views_p50")]
    public long VideoViewsP50 { get; set; }

    [JsonPropertyName("video_views_p75")]
    public long VideoViewsP75 { get; set; }

    [JsonPropertyName("video_views_p100")]
    public long VideoViewsP100 { get; set; }

    [JsonPropertyName("traffic_source_types")]
    public Dictionary<string, double> TrafficSourceTypes { get; set; } = new();
}
