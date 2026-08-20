using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.DTOs.OAuth;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Api;

/// <summary>
/// OAuth against TikTok API for Business — a separate authorization server from Login Kit,
/// so this cannot reuse <see cref="TikTokOAuthClient"/>.
///
/// Like that class it deliberately does NOT extend TikTokApiClientBase: the base writes full
/// response bodies to the api_call_log table, and these responses carry access tokens.
///
/// The endpoint contract is unverified — TikTok's business portal docs are not publicly
/// fetchable — so the URLs come from options and the response is captured permissively.
/// If TikTok names a field differently, only the mapping below needs adjusting.
/// </summary>
public class TikTokBusinessOAuthClient : ITikTokBusinessOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly TikTokBusinessOAuthOptions _options;
    private readonly ILogger<TikTokBusinessOAuthClient> _logger;

    public TikTokBusinessOAuthClient(
        HttpClient httpClient,
        IOptions<TikTokBusinessOAuthOptions> options,
        ILogger<TikTokBusinessOAuthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var query = string.Join("&",
        [
            $"app_id={Uri.EscapeDataString(_options.AppId)}",
            $"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}",
            $"state={Uri.EscapeDataString(state)}"
        ]);

        return $"{_options.AuthorizeUrl}?{query}";
    }

    public async Task<BusinessToken> ExchangeCodeAsync(string authCode, CancellationToken ct = default)
    {
        // The business token endpoint takes a JSON body, unlike Login Kit's form encoding.
        var payload = JsonSerializer.Serialize(new
        {
            app_id = _options.AppId,
            secret = _options.Secret,
            auth_code = authCode,
            grant_type = "authorization_code"
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // Body may carry a token even on error paths — log the status only.
            _logger.LogError("TikTok business token endpoint returned {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException(
                $"TikTok business token endpoint returned {(int)response.StatusCode}.");
        }

        var parsed = JsonSerializer.Deserialize<TikTokBusinessTokenResponse>(body)
            ?? throw new JsonException("Could not deserialize the TikTok business token response.");

        // Business API reports failures as code != 0 with HTTP 200.
        if (!parsed.IsSuccess)
        {
            _logger.LogError("TikTok business token request failed: code {Code} — {Message} (request_id {RequestId})",
                parsed.Code, parsed.Message, parsed.RequestId);
            throw new InvalidOperationException(
                $"TikTok rejected the business authorization: code {parsed.Code} — {parsed.Message}");
        }

        var data = parsed.Data!;
        var now = DateTime.UtcNow;

        var businessId = data.ResolveBusinessId();
        if (string.IsNullOrWhiteSpace(businessId))
        {
            // Not fatal on its own, but ingestion cannot address an account without it.
            _logger.LogWarning(
                "Business authorization succeeded but no account identifier was found in the response. " +
                "Non-secret fields returned: {Fields}. Check which one carries the business id and map it in " +
                "TikTokBusinessTokenData.ResolveBusinessId().",
                string.Join(", ", DescribeNonSecretFields(data)));
        }

        var token = new BusinessToken
        {
            BusinessId = businessId ?? string.Empty,
            AccessToken = data.AccessToken,
            RefreshToken = data.RefreshToken,
            Scope = data.ScopeAsString(),
            ObtainedAtUtc = now,
            // Business tokens do not always report an expiry; fall back to a configured horizon
            // so the store still has something meaningful to compare against.
            ExpiresAtUtc = data.ExpiresIn is > 0
                ? now.AddSeconds(data.ExpiresIn.Value)
                : now.AddDays(_options.AssumedLifetimeDaysWhenUnknown),
            RefreshExpiresAtUtc = data.RefreshTokenExpiresIn is > 0
                ? now.AddSeconds(data.RefreshTokenExpiresIn.Value)
                : null,
            RawFields = DescribeNonSecretFields(data)
        };

        _logger.LogInformation(
            "TikTok business authorization succeeded for business_id {BusinessId}, token valid until {Expires:u}",
            token.BusinessId, token.ExpiresAtUtc);

        return token;
    }

    /// <summary>
    /// Everything the response carried except the credentials themselves. Kept so the real
    /// contract can be inspected from the token store once a live authorization happens.
    /// </summary>
    private static Dictionary<string, string> DescribeNonSecretFields(TikTokBusinessTokenData data)
    {
        var fields = new Dictionary<string, string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) fields[key] = value;
        }

        Add("business_id", data.BusinessId);
        Add("business_ids", data.BusinessIds is null ? null : string.Join(",", data.BusinessIds));
        Add("advertiser_ids", data.AdvertiserIds is null ? null : string.Join(",", data.AdvertiserIds));
        Add("open_id", data.OpenId);
        Add("scope", data.ScopeAsString());

        if (data.Extra is not null)
        {
            foreach (var (key, value) in data.Extra)
            {
                // Never persist anything token-shaped.
                if (key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                    continue;

                Add(key, value.ToString());
            }
        }

        return fields;
    }
}
