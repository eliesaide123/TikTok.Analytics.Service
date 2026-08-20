using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Infrastructure.Api;
using TikTok.Analytics.Infrastructure.BigQuery;
using TikTok.Analytics.Infrastructure.Jobs;
using TikTok.Analytics.Infrastructure.Logging;
using TikTok.Analytics.Infrastructure.Services;
using TikTok.Analytics.Infrastructure.Sql;
using TikTok.Analytics.Infrastructure.Storage;

namespace TikTok.Analytics.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<TikTokIngestionOptions>(configuration.GetSection(TikTokIngestionOptions.SectionName));
        services.Configure<TikTokOAuthOptions>(configuration.GetSection(TikTokOAuthOptions.SectionName));
        services.Configure<TikTokBusinessOAuthOptions>(configuration.GetSection(TikTokBusinessOAuthOptions.SectionName));

        // HTTP Clients
        services.AddHttpClient<ITikTokDisplayApiClient, TikTokDisplayApiClient>();
        services.AddHttpClient<ITikTokBusinessApiClient, TikTokBusinessApiClient>();
        services.AddHttpClient<ITikTokOAuthClient, TikTokOAuthClient>();
        services.AddHttpClient<ITikTokBusinessOAuthClient, TikTokBusinessOAuthClient>();

        // Repositories. Storage is swappable so the same ingestion pipeline can write to a
        // local SQL Server during development and BigQuery in the warehouse.
        var storageProvider = configuration
            .GetSection(TikTokIngestionOptions.SectionName)["StorageProvider"] ?? "SqlServer";

        if (string.Equals(storageProvider, "BigQuery", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IAnalyticsRepository, BigQueryRepository>();
        else
            services.AddSingleton<IAnalyticsRepository, SqlServerAnalyticsRepository>();

        // OAuth token + state storage.
        // Singletons: the file store serialises writes through a static gate, and pending
        // state values must outlive the request that issued them.
        services.AddSingleton<ITikTokTokenStore, FileTikTokTokenStore>();
        services.AddSingleton<IBusinessTokenStore, FileBusinessTokenStore>();
        services.AddSingleton<IOAuthStateStore, InMemoryOAuthStateStore>();

        // Services
        services.AddScoped<IApiCallLogger, ApiCallLogger>();
        services.AddScoped<ITikTokTokenResolver, TikTokTokenResolver>();
        services.AddScoped<IIngestionService, IngestionService>();

        // Quartz (background job scheduler)
        var ingestionOptions = configuration.GetSection(TikTokIngestionOptions.SectionName).Get<TikTokIngestionOptions>() ?? new TikTokIngestionOptions();
        var oauthOptions = configuration.GetSection(TikTokOAuthOptions.SectionName).Get<TikTokOAuthOptions>() ?? new TikTokOAuthOptions();

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DailyIngestionJob");
            q.AddJob<DailyIngestionJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailyIngestionTrigger")
                .WithCronSchedule(ingestionOptions.CronSchedule));

            // Access tokens live 24h, so this keeps them alive without user interaction.
            var refreshJobKey = new JobKey("TokenRefreshJob");
            q.AddJob<TokenRefreshJob>(opts => opts.WithIdentity(refreshJobKey));
            q.AddTrigger(opts => opts
                .ForJob(refreshJobKey)
                .WithIdentity("TokenRefreshTrigger")
                .WithCronSchedule(oauthOptions.RefreshCronSchedule));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
