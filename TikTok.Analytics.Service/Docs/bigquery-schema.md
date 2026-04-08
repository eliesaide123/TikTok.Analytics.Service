# BigQuery Schema — TikTok Analytics

Run these scripts in Google BigQuery to create the dataset and all required tables.

## Create Dataset

```sql
CREATE SCHEMA IF NOT EXISTS `your-gcp-project-id.tiktok_analytics`
OPTIONS (
  location = 'US',
  description = 'TikTok Analytics data ingested via TikTok.Analytics.Service'
);
```

## 1. User Profiles

Daily snapshots of TikTok user profile data.

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.user_profiles` (
  id              STRING      NOT NULL,
  page_id         STRING      NOT NULL,
  open_id         STRING,
  display_name    STRING,
  username        STRING,
  avatar_url      STRING,
  follower_count  INT64,
  following_count INT64,
  likes_count     INT64,
  video_count     INT64,
  bio_description STRING,
  is_verified     BOOL,
  snapshot_date   DATE        NOT NULL,
  ingested_at     TIMESTAMP   NOT NULL
)
PARTITION BY snapshot_date
CLUSTER BY page_id
OPTIONS (
  description = 'Daily user profile snapshots from Display API'
);
```

## 2. Videos

Video metadata and basic engagement counts (Display API).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.videos` (
  id              STRING      NOT NULL,
  page_id         STRING      NOT NULL,
  title           STRING,
  create_time     TIMESTAMP,
  view_count      INT64,
  like_count      INT64,
  comment_count   INT64,
  share_count     INT64,
  duration        INT64,
  cover_image_url STRING,
  embed_link      STRING,
  snapshot_date   DATE        NOT NULL,
  ingested_at     TIMESTAMP   NOT NULL
)
PARTITION BY snapshot_date
CLUSTER BY page_id, id
OPTIONS (
  description = 'Video metadata and engagement counts from Display API'
);
```

## 3. Account Metrics

Account-level performance metrics (Business API).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.account_metrics` (
  id              STRING      NOT NULL,
  page_id         STRING      NOT NULL,
  business_id     STRING      NOT NULL,
  metric_date     DATE        NOT NULL,
  impressions     INT64,
  reach           INT64,
  profile_views   INT64,
  video_views     INT64,
  likes           INT64,
  comments        INT64,
  shares          INT64,
  new_followers   INT64,
  followers_count INT64,
  ingested_at     TIMESTAMP   NOT NULL
)
PARTITION BY metric_date
CLUSTER BY page_id
OPTIONS (
  description = 'Account-level analytics from Business API'
);
```

## 4. Follower Growth

Daily follower gain and loss tracking (Business API).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.follower_growth` (
  id                   STRING      NOT NULL,
  page_id              STRING      NOT NULL,
  business_id          STRING      NOT NULL,
  metric_date          DATE        NOT NULL,
  followers_count      INT64,
  daily_new_followers  INT64,
  daily_lost_followers INT64,
  ingested_at          TIMESTAMP   NOT NULL
)
PARTITION BY metric_date
CLUSTER BY page_id
OPTIONS (
  description = 'Daily follower growth from Business API'
);
```

## 5. Audience Demographics

Follower demographic breakdowns — flattened to (segment_type, segment_value, percentage).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.audience_demographics` (
  id              STRING      NOT NULL,
  page_id         STRING      NOT NULL,
  business_id     STRING      NOT NULL,
  snapshot_date   DATE        NOT NULL,
  segment_type    STRING      NOT NULL,
  segment_value   STRING      NOT NULL,
  percentage      FLOAT64,
  ingested_at     TIMESTAMP   NOT NULL
)
PARTITION BY snapshot_date
CLUSTER BY page_id, segment_type
OPTIONS (
  description = 'Audience demographics from Business API (gender, age, country)'
);
```

## 6. Video Analytics

Deep per-video analytics — impressions, reach, watch time, completion quartiles (Business API).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.video_analytics` (
  id                       STRING      NOT NULL,
  page_id                  STRING      NOT NULL,
  video_id                 STRING      NOT NULL,
  impressions              INT64,
  reach                    INT64,
  view_count               INT64,
  likes                    INT64,
  comments                 INT64,
  shares                   INT64,
  saves                    INT64,
  average_watch_time       FLOAT64,
  total_watch_time         FLOAT64,
  full_video_watched_rate  FLOAT64,
  video_views_p25          INT64,
  video_views_p50          INT64,
  video_views_p75          INT64,
  video_views_p100         INT64,
  snapshot_date            DATE        NOT NULL,
  ingested_at              TIMESTAMP   NOT NULL
)
PARTITION BY snapshot_date
CLUSTER BY page_id, video_id
OPTIONS (
  description = 'Deep video analytics from Business API'
);
```

## 7. Traffic Sources

Per-video traffic source breakdown (Business API).

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.traffic_sources` (
  id              STRING      NOT NULL,
  page_id         STRING      NOT NULL,
  video_id        STRING      NOT NULL,
  source_type     STRING      NOT NULL,
  percentage      FLOAT64,
  snapshot_date   DATE        NOT NULL,
  ingested_at     TIMESTAMP   NOT NULL
)
PARTITION BY snapshot_date
CLUSTER BY page_id, video_id
OPTIONS (
  description = 'Traffic source breakdown per video from Business API'
);
```

## 8. API Call Logs

Logging table for every TikTok API call — request payload, response, duration, success/failure.

```sql
CREATE TABLE IF NOT EXISTS `your-gcp-project-id.tiktok_analytics.api_call_logs` (
  id                   STRING      NOT NULL,
  page_id              STRING,
  endpoint             STRING      NOT NULL,
  http_method          STRING      NOT NULL,
  request_url          STRING,
  request_payload      STRING,
  response_status_code INT64,
  response_payload     STRING,
  duration_ms          INT64,
  success              BOOL,
  error_message        STRING,
  called_at            TIMESTAMP   NOT NULL
)
PARTITION BY DATE(called_at)
CLUSTER BY page_id, endpoint
OPTIONS (
  description = 'API call audit log — every TikTok API request and response'
);
```

## Notes

- Replace `your-gcp-project-id` with your actual GCP project ID.
- All tables use **date partitioning** for cost-effective querying.
- All tables are **clustered by page_id** for multi-account performance.
- The `api_call_logs` table stores full request/response payloads (truncated to 64KB) for debugging.
- `snapshot_date` columns enable daily snapshot tracking — each day's ingestion creates new rows, preserving historical data.
- The 60-day lookback window means video metrics are re-ingested daily, allowing you to track metric changes over time.
