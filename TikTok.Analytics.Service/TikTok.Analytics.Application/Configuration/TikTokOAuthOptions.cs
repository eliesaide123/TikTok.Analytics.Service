namespace TikTok.Analytics.Application.Configuration;

public class TikTokOAuthOptions
{
    public const string SectionName = "TikTokOAuth";

    /// <summary>Client key from the TikTok developer portal (App details).</summary>
    public string ClientKey { get; set; } = string.Empty;

    /// <summary>Client secret from the TikTok developer portal. Keep out of source control.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Must match a Redirect URI registered under Login Kit byte for byte, including
    /// scheme and any trailing slash, or TikTok rejects the authorization.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Scopes requested at authorize time. Each must also be granted to the app in the portal.</summary>
    public List<string> Scopes { get; set; } =
    [
        "user.info.basic",
        "user.info.profile",
        "user.info.stats",
        "video.list"
    ];

    /// <summary>Where the token store persists tokens. Relative paths resolve against the content root.</summary>
    public string TokenStorePath { get; set; } = "tiktok-tokens.json";

    /// <summary>Cron for the refresh job. Access tokens live 24h, so twice daily leaves margin.</summary>
    public string RefreshCronSchedule { get; set; } = "0 0 */12 * * ?";

    /// <summary>Refresh anything expiring within this window on the next job run.</summary>
    public int RefreshIfExpiringWithinMinutes { get; set; } = 120;

    /// <summary>How long an unconsumed CSRF state value stays valid.</summary>
    public int StateLifetimeMinutes { get; set; } = 10;
}
