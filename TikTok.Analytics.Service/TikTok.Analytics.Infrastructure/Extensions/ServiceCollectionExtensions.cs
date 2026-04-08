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

namespace TikTok.Analytics.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<TikTokIngestionOptions>(configuration.GetSection(TikTokIngestionOptions.SectionName));

        // HTTP Clients
        services.AddHttpClient<ITikTokDisplayApiClient, TikTokDisplayApiClient>();
        services.AddHttpClient<ITikTokBusinessApiClient, TikTokBusinessApiClient>();

        // Repositories
        services.AddSingleton<IBigQueryRepository, BigQueryRepository>();

        // Services
        services.AddScoped<IApiCallLogger, ApiCallLogger>();
        services.AddScoped<IIngestionService, IngestionService>();

        // Quartz (background job scheduler)
        var ingestionOptions = configuration.GetSection(TikTokIngestionOptions.SectionName).Get<TikTokIngestionOptions>() ?? new TikTokIngestionOptions();

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DailyIngestionJob");
            q.AddJob<DailyIngestionJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailyIngestionTrigger")
                .WithCronSchedule(ingestionOptions.CronSchedule));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
