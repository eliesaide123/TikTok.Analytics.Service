using Microsoft.Extensions.Logging;
using Quartz;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class DailyIngestionJob : IJob
{
    private readonly IIngestionService _ingestionService;
    private readonly ILogger<DailyIngestionJob> _logger;

    public DailyIngestionJob(IIngestionService ingestionService, ILogger<DailyIngestionJob> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Daily TikTok ingestion job started at {Time}", DateTime.UtcNow);
        try
        {
            await _ingestionService.RunIngestionAsync(context.CancellationToken);
            _logger.LogInformation("Daily TikTok ingestion job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily TikTok ingestion job failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
