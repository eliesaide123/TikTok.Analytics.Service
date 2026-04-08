namespace TikTok.Analytics.Application.Interfaces;

public interface IIngestionService
{
    Task RunIngestionAsync(CancellationToken ct = default);
}
