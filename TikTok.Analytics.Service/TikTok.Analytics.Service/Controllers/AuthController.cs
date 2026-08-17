using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.API.Controllers;

[ApiController]
[Route("api/auth/tiktok")]
public class AuthController : ControllerBase
{
    private readonly ITikTokOAuthClient _oauthClient;
    private readonly ITikTokTokenStore _tokenStore;
    private readonly IOAuthStateStore _stateStore;
    private readonly TikTokOAuthOptions _options;
    private readonly TikTokIngestionOptions _ingestionOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITikTokOAuthClient oauthClient,
        ITikTokTokenStore tokenStore,
        IOAuthStateStore stateStore,
        IOptions<TikTokOAuthOptions> options,
        IOptions<TikTokIngestionOptions> ingestionOptions,
        ILogger<AuthController> logger)
    {
        _oauthClient = oauthClient;
        _tokenStore = tokenStore;
        _stateStore = stateStore;
        _options = options.Value;
        _ingestionOptions = ingestionOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Starts the flow. Open this in a browser to send the user to TikTok's consent screen.
    /// </summary>
    /// <param name="pageId">
    /// Which configured page this authorization belongs to. Supplying it is what lets the
    /// ingestion job find the resulting token, so it is effectively required in practice.
    /// </param>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? pageId)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientKey) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return Problem(
                title: "TikTok OAuth is not configured",
                detail: "Set TikTokOAuth:ClientKey and TikTokOAuth:ClientSecret before starting the login flow.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            return Problem(
                title: "TikTok OAuth is not configured",
                detail: "Set TikTokOAuth:RedirectUri to the exact URI registered under Login Kit.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // Fail fast on an unknown page rather than storing a token the ingestion job
        // will never look up.
        if (!string.IsNullOrWhiteSpace(pageId) &&
            _ingestionOptions.Pages.All(p => p.PageId != pageId))
        {
            return BadRequest(new
            {
                error = "unknown_page_id",
                errorDescription = $"'{pageId}' is not in TikTokIngestion:Pages.",
                configuredPageIds = _ingestionOptions.Pages.Select(p => p.PageId)
            });
        }

        if (string.IsNullOrWhiteSpace(pageId))
        {
            _logger.LogWarning("Authorization started without a pageId. The resulting token will not be " +
                               "attached to any page, so ingestion will not use it.");
        }

        var state = _stateStore.Issue(pageId);
        var authorizeUrl = _oauthClient.BuildAuthorizeUrl(state);

        _logger.LogInformation("Redirecting to TikTok consent screen for page {PageId} with scopes: {Scopes}",
            pageId ?? "(none)", string.Join(", ", _options.Scopes));

        return Redirect(authorizeUrl);
    }

    /// <summary>
    /// The registered Redirect URI. TikTok sends the browser back here with ?code and ?state,
    /// or with ?error when the user declines.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("TikTok authorization declined or failed: {Error} — {Description}", error, errorDescription);
            return BadRequest(new { error, errorDescription });
        }

        // Verify state before touching the code — this is the CSRF check. Redeeming also
        // returns the PageId this authorization was started for.
        if (!_stateStore.TryRedeem(state ?? string.Empty, out var pageId))
        {
            _logger.LogWarning("Rejected TikTok callback: state missing, expired, or already used.");
            return BadRequest(new
            {
                error = "invalid_state",
                errorDescription = "The state value was missing, expired, or already redeemed. Restart at /api/auth/tiktok/login."
            });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { error = "missing_code", errorDescription = "TikTok did not return an authorization code." });
        }

        try
        {
            var token = await _oauthClient.ExchangeCodeAsync(code, ct);

            // Stamping the PageId is what makes this token discoverable by the ingestion job.
            token.PageId = pageId;
            await _tokenStore.SaveAsync(token, ct);

            _logger.LogInformation("Authorized open_id {OpenId} for page {PageId}", token.OpenId, pageId ?? "(none)");

            // Never return the token itself to the browser.
            return Ok(new
            {
                status = "authorized",
                openId = token.OpenId,
                pageId = token.PageId,
                scope = token.Scope,
                accessTokenExpiresAtUtc = token.ExpiresAtUtc,
                refreshTokenExpiresAtUtc = token.RefreshExpiresAtUtc,
                warning = string.IsNullOrWhiteSpace(pageId)
                    ? "No pageId was supplied, so the ingestion job will not use this token. " +
                      "Re-authorize via /api/auth/tiktok/login?pageId=<your-page-id>."
                    : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok authorization code exchange failed.");
            return Problem(
                title: "Token exchange failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Which accounts are authorized and how long their credentials remain valid.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tokens = await _tokenStore.GetAllAsync(ct);

        return Ok(tokens.Select(t => new
        {
            openId = t.OpenId,
            pageId = t.PageId,
            scope = t.Scope,
            accessTokenExpiresAtUtc = t.ExpiresAtUtc,
            accessTokenExpired = t.IsAccessExpired(now),
            refreshTokenExpiresAtUtc = t.RefreshExpiresAtUtc,
            refreshTokenExpired = t.IsRefreshExpired(now),
            obtainedAtUtc = t.ObtainedAtUtc
        }));
    }

    /// <summary>Forces a refresh for one account, rather than waiting for the scheduled job.</summary>
    [HttpPost("refresh/{openId}")]
    public async Task<IActionResult> Refresh(string openId, CancellationToken ct)
    {
        var existing = await _tokenStore.GetAsync(openId, ct);
        if (existing is null)
            return NotFound(new { error = "unknown_open_id", openId });

        if (existing.IsRefreshExpired(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                error = "refresh_token_expired",
                errorDescription = "The refresh token has expired. This account must re-authorize at /api/auth/tiktok/login."
            });
        }

        try
        {
            var updated = await _oauthClient.RefreshAsync(existing.RefreshToken, ct);
            if (string.IsNullOrEmpty(updated.OpenId))
                updated.OpenId = existing.OpenId;
            updated.PageId = existing.PageId;

            await _tokenStore.SaveAsync(updated, ct);

            return Ok(new
            {
                status = "refreshed",
                openId = updated.OpenId,
                accessTokenExpiresAtUtc = updated.ExpiresAtUtc,
                refreshTokenExpiresAtUtc = updated.RefreshExpiresAtUtc
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual token refresh failed for open_id {OpenId}", openId);
            return Problem(title: "Refresh failed", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
