namespace TikTok.Analytics.Application.Configuration;

/// <summary>
/// Credentials for the TikTok API for Business app — a different app, in a different
/// portal, from the Login Kit one in <see cref="TikTokOAuthOptions"/>. Registered at
/// business-api.tiktok.com/portal; the Display API app will not work here.
/// </summary>
public class TikTokBusinessOAuthOptions
{
    public const string SectionName = "TikTokBusinessOAuth";

    /// <summary>App ID from the TikTok for Business developer portal.</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>App secret from the same portal. Keep out of source control.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Must byte-for-byte match the redirect URI registered against the app.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Endpoints are configurable rather than constants: TikTok's business portal
    /// documentation is not publicly fetchable, so these defaults are the expected shape
    /// and may need correcting against the portal without a code change.
    /// </summary>
    public string AuthorizeUrl { get; set; } = "https://business-api.tiktok.com/portal/auth";

    public string TokenUrl { get; set; } = "https://business-api.tiktok.com/open_api/v1.3/oauth2/access_token/";

    /// <summary>Where business tokens are persisted. Relative paths resolve against the content root.</summary>
    public string TokenStorePath { get; set; } = "tiktok-business-tokens.json";

    /// <summary>How long an unconsumed CSRF state value stays valid.</summary>
    public int StateLifetimeMinutes { get; set; } = 10;

    /// <summary>
    /// Business tokens may come back without an expiry. When that happens, assume this
    /// many days so the store still carries a sane refresh horizon.
    /// </summary>
    public int AssumedLifetimeDaysWhenUnknown { get; set; } = 365;
}
