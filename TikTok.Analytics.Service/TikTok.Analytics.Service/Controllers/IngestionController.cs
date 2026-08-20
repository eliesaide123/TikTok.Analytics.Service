using Microsoft.AspNetCore.Mvc;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.API.Controllers;

/// <summary>
/// Runs the ingestion pipeline on demand. The scheduled job owns normal operation; this
/// exists so a run can be triggered and verified without waiting for the nightly cron.
/// </summary>
[ApiController]
[Route("api/ingestion")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionService _ingestionService;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(IIngestionService ingestionService, ILogger<IngestionController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("Manual ingestion run requested");

        try
        {
            await _ingestionService.RunIngestionAsync(ct);

            return Ok(new
            {
                status = "completed",
                startedAtUtc = startedAt,
                finishedAtUtc = DateTime.UtcNow,
                note = "Per-page failures are logged and skipped rather than failing the run — check the logs and row counts."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual ingestion run failed");
            return Problem(title: "Ingestion failed", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
