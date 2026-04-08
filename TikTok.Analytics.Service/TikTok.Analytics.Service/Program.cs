using TikTok.Analytics.Application.Mapping;
using TikTok.Analytics.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// AutoMapper
builder.Services.AddAutoMapper(typeof(TikTokMappingProfile));

// Infrastructure (HTTP clients, BigQuery, Quartz scheduler, config)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
