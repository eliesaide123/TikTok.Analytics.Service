namespace TikTok.Analytics.Application.Configuration;

public class TikTokIngestionOptions
{
    public const string SectionName = "TikTokIngestion";

    public DateTime HistoryStartDate { get; set; } = new(2026, 1, 1);
    public int LookbackDays { get; set; } = 60;
    public string CronSchedule { get; set; } = "0 0 0 * * ?";
    /// <summary>Which store ingested data is written to: "SqlServer" or "BigQuery".</summary>
    public string StorageProvider { get; set; } = "SqlServer";

    public BigQueryOptions BigQuery { get; set; } = new();
    public SqlServerOptions SqlServer { get; set; } = new();
    public List<PageConfig> Pages { get; set; } = new();
}

public class SqlServerOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class BigQueryOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = "tiktok_analytics";
}

public class PageConfig
{
    public string PageId { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty;
    public string DisplayAccessToken { get; set; } = string.Empty;
    public string BusinessAccessToken { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
