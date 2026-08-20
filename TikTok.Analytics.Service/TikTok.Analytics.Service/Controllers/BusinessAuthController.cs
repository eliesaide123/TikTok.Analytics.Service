using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.API.Controllers;

/// <summary>
/// Authorization against TikTok API for Business. Parallel to <see cref="AuthController"/>
/// but a different app in a different portal, so the two flows share nothing but the
/// state store.
/// </summary>
[ApiController]
[Route("api/auth/tiktok/business")]
public class BusinessAuthController : ControllerBase
{
    private readonly ITikTokBusinessOAuthClient _oauthClient;
    private readonly IBusinessTokenStore _tokenStore;
    private readonly IOAuthStateStore _stateStore;
    private readonly TikTokBusinessOAuthOptions _options;
    private readonly TikTokIngestionOptions _ingestionOptions;
    private readonly ILogger<BusinessAuthController> _logger;

    public BusinessAuthController(
        ITikTokBusinessOAuthClient oauthClient,
        IBusinessTokenStore tokenStore,
        IOAuthStateStore stateStore,
        IOptions<TikTokBusinessOAuthOptions> options,
        IOptions<TikTokIngestionOptions> ingestionOptions,
        ILogger<BusinessAuthController> logger)
    {
        _oauthClient = oauthClient;
        _tokenStore = tokenStore;
        _stateStore = stateStore;
        _options = options.Value;
        _ingestionOptions = ingestionOptions.Value;
        _logger = logger;
    }

    /// <summary>Open in a browser to start the Business API consent flow.</summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? pageId)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.Secret))
        {
            return Problem(
                title: "TikTok Business OAuth is not configured",
                detail: "Set TikTokBusinessOAuth:AppId and TikTokBusinessOAuth:Secret from " +
                        "business-api.tiktok.com/portal before starting this flow.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            return Problem(
                title: "TikTok Business OAuth is not configured",
                detail: "Set TikTokBusinessOAuth:RedirectUri to the exact URI registered against the app.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // Fail fast on an unknown page rather than storing a credential ingestion cannot find.
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

        var state = _stateStore.Issue(pageId);
        var url = _oauthClient.BuildAuthorizeUrl(state);

        _logger.LogInformation("Redirecting to TikTok Business consent screen for page {PageId}", pageId ?? "(none)");
        return Redirect(url);
    }

    /// <summary>
    /// The registered redirect URI. TikTok returns an auth code here — note the parameter is
    /// auth_code on this API, not code as in Login Kit.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery(Name = "auth_code")] string? authCode,
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        // Accept either spelling so a contract difference does not silently drop the code.
        var receivedCode = !string.IsNullOrWhiteSpace(authCode) ? authCode : code;

        if (!_stateStore.TryRedeem(state ?? string.Empty, out var pageId))
        {
            _logger.LogWarning("Rejected TikTok business callback: state missing, expired, or already used.");
            return BadRequest(new
            {
                error = "invalid_state",
                errorDescription = "The state value was missing, expired, or already redeemed. " +
                                   "Restart at /api/auth/tiktok/business/login."
            });
        }

        if (string.IsNullOrWhiteSpace(receivedCode))
            return BadRequest(new { error = "missing_code", errorDescription = "TikTok did not return an authorization code." });

        try
        {
            var token = await _oauthClient.ExchangeCodeAsync(receivedCode, ct);
            token.PageId = pageId;

            if (string.IsNullOrWhiteSpace(token.BusinessId))
            {
                // Without an account identifier the credential cannot address /business/get/.
                // Surface what TikTok did return so the mapping can be corrected.
                return Ok(new
                {
                    status = "authorized_but_unusable",
                    warning = "No business_id was found in the response, so ingestion cannot use this credential yet.",
                    fieldsReturned = token.RawFields,
                    nextStep = "Identify which field carries the account id and map it in TikTokBusinessTokenData.ResolveBusinessId()."
                });
            }

            await _tokenStore.SaveAsync(token, ct);
            _logger.LogInformation("Authorized business_id {BusinessId} for page {PageId}", token.BusinessId, pageId ?? "(none)");

            // Never return the token itself to the browser.
            return Ok(new
            {
                status = "authorized",
                businessId = token.BusinessId,
                pageId = token.PageId,
                scope = token.Scope,
                expiresAtUtc = token.ExpiresAtUtc,
                fieldsReturned = token.RawFields,
                warning = string.IsNullOrWhiteSpace(pageId)
                    ? "No pageId was supplied, so ingestion will not use this credential."
                    : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TikTok business authorization failed.");
            return Problem(title: "Business token exchange failed", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Which business accounts are authorized, and until when.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tokens = await _tokenStore.GetAllAsync(ct);

        return Ok(tokens.Select(t => new
        {
            businessId = t.BusinessId,
            pageId = t.PageId,
            scope = t.Scope,
            expiresAtUtc = t.ExpiresAtUtc,
            expired = t.IsExpired(now),
            obtainedAtUtc = t.ObtainedAtUtc,
            fieldsReturned = t.RawFields
        }));
    }
}
