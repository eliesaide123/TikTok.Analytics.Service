# TikTok Analytics Service — Project Documentation

## Overview

A .NET 8 background service that ingests TikTok analytics data from the Display API and Business API on a daily schedule and persists it into Google BigQuery. Built using Clean Architecture with four layers: Domain, Application, Infrastructure, and API.

---

## Table of Contents

1. [Architecture](#architecture)
2. [Solution Structure](#solution-structure)
3. [Configuration](#configuration)
4. [Background Job & Scheduling](#background-job--scheduling)
5. [Ingestion Pipeline](#ingestion-pipeline)
6. [TikTok API Clients](#tiktok-api-clients)
7. [Domain Entities](#domain-entities)
8. [DTOs & Mapping](#dtos--mapping)
9. [BigQuery Repository](#bigquery-repository)
10. [API Call Logging](#api-call-logging)
11. [Dependency Injection](#dependency-injection)
12. [BigQuery Tables](#bigquery-tables)
13. [Getting Started](#getting-started)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  TikTok.Analytics.API  (ASP.NET Core Web Host)                  │
│  - Program.cs (clean, minimal)                                  │
│  - /health endpoint                                             │
│  - Quartz.AspNetCore hosted service                             │
├─────────────────────────────────────────────────────────────────┤
│  TikTok.Analytics.Infrastructure                                │
│  - TikTok API HTTP clients (Display + Business)                 │
│  - BigQuery repository                                          │
│  - IngestionService (orchestrator)                              │
│  - DailyIngestionJob (Quartz scheduler)                         │
│  - ApiCallLogger                                                │
├─────────────────────────────────────────────────────────────────┤
│  TikTok.Analytics.Application                                   │
│  - Interfaces (contracts)                                       │
│  - DTOs (API response shapes)                                   │
│  - Configuration models                                         │
│  - AutoMapper profiles                                          │
├─────────────────────────────────────────────────────────────────┤
│  TikTok.Analytics.Domain                                        │
│  - Entities (pure data models)                                  │
│  - Enums                                                        │
└─────────────────────────────────────────────────────────────────┘
```

**Dependency flow:** API → Infrastructure → Application → Domain

---

## Solution Structure

```
TikTok.Analytics.Service/
├── TikTok.Analytics.Service.slnx
│
├── Docs/
│   ├── bigquery-schema.md                  # BigQuery DDL scripts (8 tables)
│   └── project-documentation.md            # This file
│
├── TikTok.Analytics.Domain/
│   ├── TikTok.Analytics.Domain.csproj      # No dependencies
│   ├── Entities/
│   │   ├── UserProfile.cs
│   │   ├── Video.cs
│   │   ├── AccountMetrics.cs
│   │   ├── FollowerGrowth.cs
│   │   ├── AudienceDemographic.cs
│   │   ├── VideoAnalytics.cs
│   │   ├── TrafficSource.cs
│   │   └── ApiCallLog.cs
│   └── Enums/
│       ├── TrafficSourceType.cs
│       └── DemographicSegmentType.cs
│
├── TikTok.Analytics.Application/
│   ├── TikTok.Analytics.Application.csproj  # AutoMapper, Logging, Options
│   ├── Configuration/
│   │   └── TikTokIngestionOptions.cs        # All config models
│   ├── DTOs/
│   │   ├── Display/
│   │   │   └── TikTokDisplayResponses.cs    # Display API response DTOs
│   │   └── Business/
│   │       └── TikTokBusinessResponses.cs   # Business API response DTOs
│   ├── Interfaces/
│   │   ├── ITikTokDisplayApiClient.cs
│   │   ├── ITikTokBusinessApiClient.cs
│   │   ├── IBigQueryRepository.cs
│   │   ├── IIngestionService.cs
│   │   └── IApiCallLogger.cs
│   └── Mapping/
│       └── TikTokMappingProfile.cs          # AutoMapper DTO → Entity maps
│
├── TikTok.Analytics.Infrastructure/
│   ├── TikTok.Analytics.Infrastructure.csproj  # BigQuery, Quartz, HttpClient
│   ├── Api/
│   │   ├── TikTokApiClientBase.cs           # Base HTTP client with logging
│   │   ├── TikTokDisplayApiClient.cs        # Display API (v2)
│   │   └── TikTokBusinessApiClient.cs       # Business API (v1.3)
│   ├── BigQuery/
│   │   └── BigQueryRepository.cs            # All 8 table insert methods
│   ├── Logging/
│   │   └── ApiCallLogger.cs                 # Persists API call logs to BQ
│   ├── Services/
│   │   └── IngestionService.cs              # Core orchestrator
│   ├── Jobs/
│   │   └── DailyIngestionJob.cs             # Quartz scheduled job
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs   # DI registration
│
└── TikTok.Analytics.Service/                # API host project
    ├── TikTok.Analytics.API.csproj          # Quartz.AspNetCore
    ├── Program.cs                           # 8 lines, clean
    ├── appsettings.json                     # Full config with pages
    └── appsettings.Development.json         # Debug logging
```

---

## Configuration

All configuration lives in `appsettings.json` under the `TikTokIngestion` section.

```json
{
  "TikTokIngestion": {
    "HistoryStartDate": "2026-01-01",
    "LookbackDays": 60,
    "CronSchedule": "0 0 0 * * ?",
    "BigQuery": {
      "ProjectId": "your-gcp-project-id",
      "DatasetId": "tiktok_analytics"
    },
    "Pages": [
      {
        "PageId": "page_brand_1",
        "PageName": "My Brand TikTok",
        "BusinessId": "YOUR_BUSINESS_ID",
        "DisplayAccessToken": "YOUR_DISPLAY_OAUTH_TOKEN",
        "BusinessAccessToken": "YOUR_BUSINESS_OAUTH_TOKEN",
        "Enabled": true
      }
    ]
  }
}
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HistoryStartDate` | DateTime | `2026-01-01` | Earliest date to ingest data from. No data before this date is fetched. |
| `LookbackDays` | int | `60` | Number of days to look back for posts and metrics on each run. |
| `CronSchedule` | string | `0 0 0 * * ?` | Quartz cron expression. Default = midnight daily. |
| `BigQuery.ProjectId` | string | — | Your Google Cloud project ID. |
| `BigQuery.DatasetId` | string | `tiktok_analytics` | BigQuery dataset name. |
| `Pages` | array | — | List of TikTok accounts to ingest. |

### Page Configuration

Each entry in `Pages` represents a TikTok account:

| Property | Type | Description |
|----------|------|-------------|
| `PageId` | string | Unique identifier for this page in your system. |
| `PageName` | string | Human-readable name for logging. |
| `BusinessId` | string | TikTok Business Account ID. |
| `DisplayAccessToken` | string | OAuth token for Display API (any account type). |
| `BusinessAccessToken` | string | OAuth token for Business API (Business accounts only). |
| `Enabled` | bool | Set `false` to skip this page during ingestion. |

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Quartz": "Information",
      "TikTok.Analytics": "Information"
    }
  }
}
```

In Development, all TikTok.Analytics and Quartz logs are set to `Debug`.

---

## Background Job & Scheduling

The daily ingestion runs via **Quartz.NET**, a mature .NET job scheduler.

- **Job class:** `DailyIngestionJob` (`Infrastructure/Jobs/`)
- **Default schedule:** `0 0 0 * * ?` — every day at **12:00 AM UTC**
- **Concurrency:** `[DisallowConcurrentExecution]` prevents overlapping runs
- **Error handling:** Failed jobs are logged and wrapped in `JobExecutionException` (no immediate refire)
- **Hosted service:** Quartz runs as an `IHostedService` via `AddQuartzHostedService`

To change the schedule, modify `CronSchedule` in `appsettings.json`. Examples:

| Cron Expression | Schedule |
|----------------|----------|
| `0 0 0 * * ?` | Midnight daily |
| `0 0 6 * * ?` | 6:00 AM daily |
| `0 0 0/12 * * ?` | Every 12 hours |
| `0 0 0 ? * MON-FRI` | Midnight weekdays only |

---

## Ingestion Pipeline

The `IngestionService` is the core orchestrator. On each run:

### Date Window Calculation

```
today = DateTime.UtcNow.Date
lookbackDate = today - LookbackDays (60)
effectiveStartDate = Max(HistoryStartDate, lookbackDate)
```

Example (today = 2026-03-17):
- `lookbackDate` = 2026-01-16
- `HistoryStartDate` = 2026-01-01
- `effectiveStartDate` = **2026-01-16** (lookback wins)

Example (today = 2026-02-15):
- `lookbackDate` = 2025-12-17
- `HistoryStartDate` = 2026-01-01
- `effectiveStartDate` = **2026-01-01** (history cutoff wins)

### Per-Page Execution Flow

For each **enabled** page in configuration:

| Step | Method | API | Data |
|------|--------|-----|------|
| 1 | `IngestUserProfileAsync` | Display API | User profile snapshot |
| 2 | `IngestVideosAsync` | Display API | Video list (paginated, filtered by date) |
| 3 | `IngestAccountMetricsAsync` | Business API | Impressions, reach, engagement totals |
| 4 | `IngestFollowerGrowthAsync` | Business API | Daily follower gain/loss |
| 5 | `IngestDemographicsAsync` | Business API | Gender, age, country breakdowns |
| 6 | `IngestVideoAnalyticsAsync` | Business API | Per-video deep metrics + traffic sources |

### Error Handling

- Each page is processed in a try/catch — if one page fails, the job continues to the next
- Individual API call failures are logged with full context
- The job itself catches all exceptions and wraps them in `JobExecutionException`

### 60-Day Lookback Logic

Every day the job runs, it re-ingests the last 60 days of data. This means:
- Video metadata and engagement counts are re-captured daily (metrics change over time)
- Video analytics (impressions, reach, watch time, quartiles) are snapshotted daily
- Each daily run creates **new rows** with a new `snapshot_date`, preserving the full history of how metrics evolved

---

## TikTok API Clients

### Base Client (`TikTokApiClientBase`)

All API clients extend this abstract base class which provides:
- HTTP request execution via `HttpClient`
- Request/response logging via `ILogger`
- API call persistence via `IApiCallLogger`
- Duration tracking via `Stopwatch`
- JSON deserialization via `System.Text.Json`

### Display API Client

| Base URL | Auth |
|----------|------|
| `https://open.tiktokapis.com/v2` | `Authorization: Bearer {token}` |

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GetUserInfoAsync` | `GET /v2/user/info/` | User profile + follower counts |
| `GetVideoListAsync` | `GET /v2/video/list/` | Paginated video list (max 20/page) |

**Fields requested for user info:**
`open_id`, `display_name`, `username`, `avatar_url`, `follower_count`, `following_count`, `likes_count`, `video_count`, `bio_description`, `is_verified`

**Fields requested for video list:**
`id`, `title`, `create_time`, `like_count`, `comment_count`, `share_count`, `view_count`, `duration`, `cover_image_url`, `embed_link`

### Business API Client

| Base URL | Auth |
|----------|------|
| `https://business-api.tiktok.com/open_api/v1.3` | `Access-Token: {token}` header |

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GetAccountMetricsAsync` | `GET /business/get/` | Impressions, reach, engagement |
| `GetFollowerGrowthAsync` | `GET /business/get/` | Daily follower gain/loss |
| `GetAudienceDemographicsAsync` | `GET /business/get/` | Gender, age, country distributions |
| `GetVideoAnalyticsAsync` | `GET /business/video/list/` | Per-video deep analytics + traffic sources |

---

## Domain Entities

All entities live in `TikTok.Analytics.Domain.Entities`.

### UserProfile
Daily snapshot of a TikTok user's profile.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `OpenId` | string | TikTok user ID |
| `DisplayName` | string | Display name |
| `Username` | string | @handle |
| `AvatarUrl` | string | Profile picture URL |
| `FollowerCount` | long | Total followers |
| `FollowingCount` | long | Total following |
| `LikesCount` | long | Total likes received |
| `VideoCount` | long | Total videos posted |
| `BioDescription` | string | Bio text |
| `IsVerified` | bool | Verification status |
| `SnapshotDate` | DateTime | Date of snapshot |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### Video
Video metadata and basic engagement counts from the Display API.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | TikTok video ID |
| `PageId` | string | Config page identifier |
| `Title` | string | Video title/caption |
| `CreateTime` | DateTime | Video publish time |
| `ViewCount` | long | Total views |
| `LikeCount` | long | Total likes |
| `CommentCount` | long | Total comments |
| `ShareCount` | long | Total shares |
| `Duration` | int | Duration in seconds |
| `CoverImageUrl` | string | Thumbnail URL |
| `EmbedLink` | string | Embed URL |
| `SnapshotDate` | DateTime | Date of snapshot |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### AccountMetrics
Account-level performance metrics from the Business API.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `BusinessId` | string | TikTok Business ID |
| `MetricDate` | DateTime | Date of the metrics |
| `Impressions` | long | Feed appearances |
| `Reach` | long | Unique viewers |
| `ProfileViews` | long | Profile page visits |
| `VideoViews` | long | Total video views |
| `Likes` | long | Total likes |
| `Comments` | long | Total comments |
| `Shares` | long | Total shares |
| `NewFollowers` | long | New followers gained |
| `FollowersCount` | long | Total follower count |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### FollowerGrowth
Daily follower gain and loss tracking.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `BusinessId` | string | TikTok Business ID |
| `MetricDate` | DateTime | Date of the metrics |
| `FollowersCount` | long | Total followers (canonical) |
| `DailyNewFollowers` | long | Followers gained |
| `DailyLostFollowers` | long | Followers lost |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### AudienceDemographic
Flattened demographic breakdown — one row per segment value.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `BusinessId` | string | TikTok Business ID |
| `SnapshotDate` | DateTime | Date of snapshot |
| `SegmentType` | string | `"gender"`, `"age"`, or `"country"` |
| `SegmentValue` | string | e.g. `"Male"`, `"18-24"`, `"US"` |
| `Percentage` | double | Percentage (0.0–1.0) |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### VideoAnalytics
Deep per-video analytics from the Business API.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `VideoId` | string | TikTok video ID |
| `Impressions` | long | Feed appearances |
| `Reach` | long | Unique viewers |
| `ViewCount` | long | Total views |
| `Likes` | long | Total likes |
| `Comments` | long | Total comments |
| `Shares` | long | Total shares |
| `Saves` | long | Total saves |
| `AverageWatchTime` | double | Avg seconds watched per view |
| `TotalWatchTime` | double | Cumulative watch time |
| `FullVideoWatchedRate` | double | Completion rate (0.0–1.0) |
| `VideoViewsP25` | long | Views reaching 25% |
| `VideoViewsP50` | long | Views reaching 50% |
| `VideoViewsP75` | long | Views reaching 75% |
| `VideoViewsP100` | long | Views reaching 100% |
| `SnapshotDate` | DateTime | Date of snapshot |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### TrafficSource
Per-video traffic source breakdown.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `VideoId` | string | TikTok video ID |
| `SourceType` | string | `FOR_YOU`, `FOLLOWING`, `PROFILE`, `SEARCH`, `SOUND`, `HASHTAG` |
| `Percentage` | double | Percentage of views from this source |
| `SnapshotDate` | DateTime | Date of snapshot |
| `IngestedAt` | DateTime | UTC timestamp of ingestion |

### ApiCallLog
Audit log for every TikTok API call.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Auto-generated GUID |
| `PageId` | string | Config page identifier |
| `Endpoint` | string | API endpoint name |
| `HttpMethod` | string | GET or POST |
| `RequestUrl` | string | Full request URL |
| `RequestPayload` | string | Request body (if any) |
| `ResponseStatusCode` | int | HTTP status code |
| `ResponsePayload` | string | Response body (truncated to 64KB) |
| `DurationMs` | long | Call duration in milliseconds |
| `Success` | bool | Whether the call succeeded |
| `ErrorMessage` | string | Error details (if failed) |
| `CalledAt` | DateTime | UTC timestamp of the call |

---

## DTOs & Mapping

### Display API DTOs

```
DisplayApiResponse<T>
├── Data: T
└── Error: DisplayApiError (Code, Message, LogId)

UserInfoData
└── User: UserInfoDto

VideoListData
├── Videos: List<VideoDto>
├── Cursor: long
└── HasMore: bool
```

### Business API DTOs

```
BusinessApiResponse<T>
├── Code: int
├── Message: string
├── RequestId: string
└── Data: T

AudienceDemographicsDto
├── AudienceGenders: List<DemographicItemDto>
├── AudienceAges: List<DemographicItemDto>
└── AudienceCountries: List<DemographicItemDto>

BusinessVideoListData
├── Videos: List<BusinessVideoDto>
├── Cursor: long
└── HasMore: bool
```

### AutoMapper Mappings (`TikTokMappingProfile`)

| Source DTO | Target Entity | Notes |
|-----------|---------------|-------|
| `UserInfoDto` | `UserProfile` | Auto-generates Id, SnapshotDate, IngestedAt |
| `VideoDto` | `Video` | Converts Unix timestamp to DateTime |
| `AccountMetricsDto` | `AccountMetrics` | Auto-generates Id, IngestedAt |
| `FollowerGrowthDto` | `FollowerGrowth` | Auto-generates Id, IngestedAt |
| `BusinessVideoDto` | `VideoAnalytics` | Maps `ItemId` → `VideoId`, auto-generates Id/dates |

**Note:** `AudienceDemographic` and `TrafficSource` entities are manually constructed in `IngestionService` (flattened from nested DTOs).

---

## BigQuery Repository

`BigQueryRepository` uses the `Google.Cloud.BigQuery.V2` SDK to insert data.

- **Registered as:** Singleton (one `BigQueryClient` instance)
- **Date formatting:** `yyyy-MM-dd` for DATE columns, `yyyy-MM-ddTHH:mm:ss.ffffffZ` for TIMESTAMP columns
- **Batch inserts:** Videos, demographics, video analytics, and traffic sources use batch inserts (`InsertRowsAsync`)
- **Single inserts:** User profile, account metrics, follower growth, API call logs use single-row inserts

### Table Mapping

| Method | BigQuery Table |
|--------|---------------|
| `InsertUserProfileAsync` | `user_profiles` |
| `InsertVideosAsync` | `videos` |
| `InsertAccountMetricsAsync` | `account_metrics` |
| `InsertFollowerGrowthAsync` | `follower_growth` |
| `InsertAudienceDemographicsAsync` | `audience_demographics` |
| `InsertVideoAnalyticsAsync` | `video_analytics` |
| `InsertTrafficSourcesAsync` | `traffic_sources` |
| `InsertApiCallLogAsync` | `api_call_logs` |

---

## API Call Logging

Every TikTok API call is logged to the `api_call_logs` BigQuery table via `ApiCallLogger`.

**What is captured:**
- Full request URL and payload
- Full response body (truncated to 64KB)
- HTTP status code
- Duration in milliseconds
- Success/failure flag
- Error message (if applicable)
- Page ID and endpoint name

**Resilience:** If logging fails (e.g. BigQuery unavailable), a warning is logged via `ILogger` but the API call itself is not affected — logging failures never block ingestion.

---

## Dependency Injection

### Registration (`Infrastructure/Extensions/ServiceCollectionExtensions.cs`)

```csharp
services.AddInfrastructure(configuration);
```

This single call registers:

| Registration | Interface | Implementation | Lifetime |
|-------------|-----------|----------------|----------|
| Configuration | `IOptions<TikTokIngestionOptions>` | Bound from config | Singleton |
| HTTP Client | `ITikTokDisplayApiClient` | `TikTokDisplayApiClient` | Transient (typed) |
| HTTP Client | `ITikTokBusinessApiClient` | `TikTokBusinessApiClient` | Transient (typed) |
| Repository | `IBigQueryRepository` | `BigQueryRepository` | Singleton |
| Service | `IApiCallLogger` | `ApiCallLogger` | Scoped |
| Service | `IIngestionService` | `IngestionService` | Scoped |
| Scheduler | Quartz `DailyIngestionJob` | Cron trigger | Hosted Service |

### Program.cs (8 lines)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoMapper(typeof(TikTokMappingProfile));
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));
app.Run();
```

---

## BigQuery Tables

All table DDL scripts are in `Docs/bigquery-schema.md`. Summary:

| Table | Partition | Cluster | Source |
|-------|-----------|---------|--------|
| `user_profiles` | `snapshot_date` | `page_id` | Display API |
| `videos` | `snapshot_date` | `page_id, id` | Display API |
| `account_metrics` | `metric_date` | `page_id` | Business API |
| `follower_growth` | `metric_date` | `page_id` | Business API |
| `audience_demographics` | `snapshot_date` | `page_id, segment_type` | Business API |
| `video_analytics` | `snapshot_date` | `page_id, video_id` | Business API |
| `traffic_sources` | `snapshot_date` | `page_id, video_id` | Business API |
| `api_call_logs` | `DATE(called_at)` | `page_id, endpoint` | Internal |

All tables use **date partitioning** for cost-effective querying and **clustering** for multi-account performance.

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Google Cloud project with BigQuery API enabled
- GCP service account with BigQuery Data Editor role
- TikTok Developer account with Display API and/or Business API access
- OAuth tokens for each TikTok page you want to ingest

### 1. Create BigQuery Tables

Run the DDL scripts from `Docs/bigquery-schema.md` in the BigQuery console. Replace `your-gcp-project-id` with your actual GCP project ID.

### 2. Configure Authentication

Set up GCP authentication via one of:
- `GOOGLE_APPLICATION_CREDENTIALS` environment variable pointing to a service account JSON key
- Application Default Credentials (`gcloud auth application-default login`)

### 3. Update Configuration

Edit `appsettings.json`:
1. Set `BigQuery.ProjectId` to your GCP project ID
2. Add your TikTok pages to the `Pages` array with valid OAuth tokens
3. Adjust `CronSchedule` if needed (default: midnight daily)

### 4. Run

```bash
cd TikTok.Analytics.Service
dotnet run
```

The service starts and:
- Exposes a health check at `GET /health`
- Runs the `DailyIngestionJob` on the configured cron schedule

### NuGet Packages

| Package | Version | Layer | Purpose |
|---------|---------|-------|---------|
| AutoMapper | 13.0.1 | Application | DTO-to-entity mapping |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | Application | ILogger interfaces |
| Microsoft.Extensions.Options | 8.0.2 | Application | IOptions binding |
| Google.Cloud.BigQuery.V2 | 3.10.0 | Infrastructure | BigQuery SDK |
| Microsoft.Extensions.Http | 8.0.1 | Infrastructure | IHttpClientFactory |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.1 | Infrastructure | IHostedService |
| Quartz | 3.13.1 | Infrastructure | Job scheduler |
| Quartz.Extensions.DependencyInjection | 3.13.1 | Infrastructure | Quartz DI |
| Quartz.Extensions.Hosting | 3.13.1 | Infrastructure | Quartz hosted service |
| Quartz.AspNetCore | 3.13.1 | API | ASP.NET Core integration |
