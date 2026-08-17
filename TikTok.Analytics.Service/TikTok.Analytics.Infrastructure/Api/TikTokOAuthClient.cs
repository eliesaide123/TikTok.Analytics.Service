using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.DTOs.OAuth;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Api;

/// <summary>
/// Talks to TikTok's OAuth endpoints.
///
/// Deliberately does NOT extend TikTokApiClientBase: that base class writes the full
/// response body into the api_call_log table, and these responses carry access and
/// refresh tokens. Credentials must never reach the analytics warehouse.
/// </summary>
public class TikTokOAuthClient : ITikTokOAuthClient
{
    private const string AuthorizeUrl = "https://www.tiktok.com/v2/auth/authorize/";
    private const string TokenUrl = "https://open.tiktokapis.com/v2/oauth/token/";

    private readonly HttpClient _httpClient;
    private readonly TikTokOAuthOptions _options;
    private readonly ILogger<TikTokOAuthClient> _logger;

    public TikTokOAuthClient(
        HttpClient httpClient,
        IOptions<TikTokOAuthOptions> options,
        ILogger<TikTokOAuthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(string state)
    {
        // Deduplicate: IConfiguration binds a List<string> by APPENDING to the property's
        // default initializer rather than replacing it, so configured scopes arrive alongside
        // the defaults. Dedupe here so TikTok never sees "user.info.basic,user.info.basic,…".
        var scopes = _options.Scopes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        // Uri.EscapeDataString rather than HttpUtility: it emits RFC 3986 percent-encoding
        // with uppercase hex and never encodes a space as "+", which is form syntax.
        var query = string.Join("&",
        [
            $"client_key={Uri.EscapeDataString(_options.ClientKey)}",
            $"scope={Uri.EscapeDataString(string.Join(",", scopes))}",
            "response_type=code",
            $"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}",
            $"state={Uri.EscapeDataString(state)}"
        ]);

        return $"{AuthorizeUrl}?{query}";
    }

    public async Task<TikTokToken> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        // TikTok percent-encodes the code in the callback query string. ASP.NET Core has
        // already decoded it once by the time it binds; decoding again here is harmless for
        // a correctly-decoded value and repairs the double-encoded case.
        var normalisedCode = HttpUtility.UrlDecode(code);

        var form = new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = normalisedCode,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri
        };

        return await PostTokenRequestAsync(form, "authorization_code", ct);
    }

    public async Task<TikTokToken> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            ["client_key"] = _options.ClientKey,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        return await PostTokenRequestAsync(form, "refresh_token", ct);
    }

    private async Task<TikTokToken> PostTokenRequestAsync(
        Dictionary<string, string> form, string grantType, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // Body may contain a token on partial success paths — log status only.
            _logger.LogError("TikTok token endpoint returned {StatusCode} for grant {GrantType}",
                (int)response.StatusCode, grantType);
            throw new HttpRequestException(
                $"TikTok token endpoint returned {(int)response.StatusCode} for grant '{grantType}'.");
        }

        var payload = JsonSerializer.Deserialize<TikTokTokenResponse>(body)
            ?? throw new JsonException("Could not deserialize the TikTok token response.");

        // TikTok reports most failures as HTTP 200 with an error field.
        if (!payload.IsSuccess)
        {
            _logger.LogError("TikTok token request failed for grant {GrantType}: {Error} — {Description} (log_id {LogId})",
                grantType, payload.Error, payload.ErrorDescription, payload.LogId);
            throw new InvalidOperationException(
                $"TikTok rejected the '{grantType}' grant: {payload.Error} — {payload.ErrorDescription}");
        }

        var now = DateTime.UtcNow;
        _logger.LogInformation("TikTok {GrantType} grant succeeded for open_id {OpenId}, access token valid {Hours}h",
            grantType, payload.OpenId, Math.Round(payload.ExpiresIn / 3600.0, 1));

        return new TikTokToken
        {
            OpenId = payload.OpenId,
            AccessToken = payload.AccessToken,
            RefreshToken = payload.RefreshToken,
            TokenType = string.IsNullOrEmpty(payload.TokenType) ? "Bearer" : payload.TokenType,
            Scope = payload.Scope,
            ExpiresAtUtc = now.AddSeconds(payload.ExpiresIn),
            RefreshExpiresAtUtc = now.AddSeconds(payload.RefreshExpiresIn),
            ObtainedAtUtc = now
        };
    }
}
