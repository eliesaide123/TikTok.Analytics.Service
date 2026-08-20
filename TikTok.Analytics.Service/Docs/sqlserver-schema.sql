/*
    TikTok Analytics — SQL Server schema.

    Mirrors the layout already used by social_analytics_IG: snake_case columns,
    a bigint identity surrogate key, datetime2 timestamps, and a tt_ table prefix.

    Every ingestion run appends a new snapshot rather than updating in place, so
    the history of how each metric moved over time is preserved. Query the latest
    state with a window function over snapshot_date, not by expecting one row.
*/

IF DB_ID('social_analytics_TK') IS NULL
    CREATE DATABASE social_analytics_TK;
GO

USE social_analytics_TK;
GO

-- ---------------------------------------------------------------- profiles --
IF OBJECT_ID('dbo.tt_user_profiles') IS NULL
CREATE TABLE dbo.tt_user_profiles (
    id               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_user_profiles PRIMARY KEY,
    record_id        nvarchar(64)   NOT NULL,
    page_id          nvarchar(64)   NOT NULL,
    open_id          nvarchar(64)   NULL,
    display_name     nvarchar(256)  NULL,
    username         nvarchar(256)  NULL,
    avatar_url       nvarchar(2048) NULL,
    follower_count   bigint         NULL,
    following_count  bigint         NULL,
    likes_count      bigint         NULL,
    video_count      bigint         NULL,
    bio_description  nvarchar(max)  NULL,
    is_verified      bit            NULL,
    snapshot_date    date           NOT NULL,
    ingested_at      datetime2      NOT NULL,
    synced_at        datetime2      NOT NULL CONSTRAINT DF_tt_user_profiles_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_user_profiles_page_date')
    CREATE INDEX IX_tt_user_profiles_page_date ON dbo.tt_user_profiles (page_id, snapshot_date DESC);
GO

-- ------------------------------------------------------------------ videos --
IF OBJECT_ID('dbo.tt_videos') IS NULL
CREATE TABLE dbo.tt_videos (
    id               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_videos PRIMARY KEY,
    video_id         nvarchar(64)   NOT NULL,
    page_id          nvarchar(64)   NOT NULL,
    title            nvarchar(max)  NULL,
    create_time      datetime2      NULL,
    view_count       bigint         NULL,
    like_count       bigint         NULL,
    comment_count    bigint         NULL,
    share_count      bigint         NULL,
    duration         int            NULL,
    cover_image_url  nvarchar(2048) NULL,
    embed_link       nvarchar(2048) NULL,
    snapshot_date    date           NOT NULL,
    ingested_at      datetime2      NOT NULL,
    synced_at        datetime2      NOT NULL CONSTRAINT DF_tt_videos_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_videos_page_video_date')
    CREATE INDEX IX_tt_videos_page_video_date ON dbo.tt_videos (page_id, video_id, snapshot_date DESC);
GO

-- --------------------------------------------------------- account metrics --
IF OBJECT_ID('dbo.tt_account_metrics') IS NULL
CREATE TABLE dbo.tt_account_metrics (
    id               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_account_metrics PRIMARY KEY,
    record_id        nvarchar(64)  NOT NULL,
    page_id          nvarchar(64)  NOT NULL,
    business_id      nvarchar(64)  NULL,
    metric_date      date          NOT NULL,
    impressions      bigint        NULL,
    reach            bigint        NULL,
    profile_views    bigint        NULL,
    video_views      bigint        NULL,
    likes            bigint        NULL,
    comments         bigint        NULL,
    shares           bigint        NULL,
    new_followers    bigint        NULL,
    followers_count  bigint        NULL,
    ingested_at      datetime2     NOT NULL,
    synced_at        datetime2     NOT NULL CONSTRAINT DF_tt_account_metrics_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_account_metrics_page_date')
    CREATE INDEX IX_tt_account_metrics_page_date ON dbo.tt_account_metrics (page_id, metric_date DESC);
GO

-- ---------------------------------------------------------- follower growth --
IF OBJECT_ID('dbo.tt_follower_growth') IS NULL
CREATE TABLE dbo.tt_follower_growth (
    id                    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_follower_growth PRIMARY KEY,
    record_id             nvarchar(64) NOT NULL,
    page_id               nvarchar(64) NOT NULL,
    business_id           nvarchar(64) NULL,
    metric_date           date         NOT NULL,
    followers_count       bigint       NULL,
    daily_new_followers   bigint       NULL,
    daily_lost_followers  bigint       NULL,
    ingested_at           datetime2    NOT NULL,
    synced_at             datetime2    NOT NULL CONSTRAINT DF_tt_follower_growth_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_follower_growth_page_date')
    CREATE INDEX IX_tt_follower_growth_page_date ON dbo.tt_follower_growth (page_id, metric_date DESC);
GO

-- ----------------------------------------------------- audience demographics --
IF OBJECT_ID('dbo.tt_audience_demographics') IS NULL
CREATE TABLE dbo.tt_audience_demographics (
    id             bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_audience_demographics PRIMARY KEY,
    record_id      nvarchar(64)  NOT NULL,
    page_id        nvarchar(64)  NOT NULL,
    business_id    nvarchar(64)  NULL,
    snapshot_date  date          NOT NULL,
    segment_type   nvarchar(32)  NOT NULL,   -- gender | age | country
    segment_value  nvarchar(128) NULL,
    percentage     float         NULL,
    ingested_at    datetime2     NOT NULL,
    synced_at      datetime2     NOT NULL CONSTRAINT DF_tt_audience_demographics_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_audience_demographics_page_type_date')
    CREATE INDEX IX_tt_audience_demographics_page_type_date
        ON dbo.tt_audience_demographics (page_id, segment_type, snapshot_date DESC);
GO

-- ----------------------------------------------------------- video analytics --
IF OBJECT_ID('dbo.tt_video_analytics') IS NULL
CREATE TABLE dbo.tt_video_analytics (
    id                       bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_video_analytics PRIMARY KEY,
    record_id                nvarchar(64) NOT NULL,
    page_id                  nvarchar(64) NOT NULL,
    video_id                 nvarchar(64) NOT NULL,
    impressions              bigint       NULL,
    reach                    bigint       NULL,
    view_count               bigint       NULL,
    likes                    bigint       NULL,
    comments                 bigint       NULL,
    shares                   bigint       NULL,
    saves                    bigint       NULL,
    average_watch_time       float        NULL,
    total_watch_time         float        NULL,
    full_video_watched_rate  float        NULL,
    video_views_p25          bigint       NULL,
    video_views_p50          bigint       NULL,
    video_views_p75          bigint       NULL,
    video_views_p100         bigint       NULL,
    snapshot_date            date         NOT NULL,
    ingested_at              datetime2    NOT NULL,
    synced_at                datetime2    NOT NULL CONSTRAINT DF_tt_video_analytics_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_video_analytics_page_video_date')
    CREATE INDEX IX_tt_video_analytics_page_video_date
        ON dbo.tt_video_analytics (page_id, video_id, snapshot_date DESC);
GO

-- ------------------------------------------------------------ traffic sources --
IF OBJECT_ID('dbo.tt_traffic_sources') IS NULL
CREATE TABLE dbo.tt_traffic_sources (
    id             bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_traffic_sources PRIMARY KEY,
    record_id      nvarchar(64) NOT NULL,
    page_id        nvarchar(64) NOT NULL,
    video_id       nvarchar(64) NOT NULL,
    source_type    nvarchar(64) NULL,       -- FOR_YOU | FOLLOWING | PROFILE | SEARCH | SOUND | HASHTAG
    percentage     float        NULL,
    snapshot_date  date         NOT NULL,
    ingested_at    datetime2    NOT NULL,
    synced_at      datetime2    NOT NULL CONSTRAINT DF_tt_traffic_sources_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_traffic_sources_page_video_date')
    CREATE INDEX IX_tt_traffic_sources_page_video_date
        ON dbo.tt_traffic_sources (page_id, video_id, snapshot_date DESC);
GO

-- ------------------------------------------------------------ api call logs --
IF OBJECT_ID('dbo.tt_api_call_logs') IS NULL
CREATE TABLE dbo.tt_api_call_logs (
    id                    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_tt_api_call_logs PRIMARY KEY,
    record_id             nvarchar(64)  NOT NULL,
    page_id               nvarchar(64)  NULL,
    endpoint              nvarchar(128) NULL,
    http_method           nvarchar(16)  NULL,
    request_url           nvarchar(max) NULL,
    request_payload       nvarchar(max) NULL,
    response_status_code  int           NULL,
    response_payload      nvarchar(max) NULL,
    duration_ms           bigint        NULL,
    success               bit           NULL,
    error_message         nvarchar(max) NULL,
    called_at             datetime2     NOT NULL,
    synced_at             datetime2     NOT NULL CONSTRAINT DF_tt_api_call_logs_synced DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tt_api_call_logs_called_at')
    CREATE INDEX IX_tt_api_call_logs_called_at ON dbo.tt_api_call_logs (called_at DESC) INCLUDE (page_id, endpoint, success);
GO
