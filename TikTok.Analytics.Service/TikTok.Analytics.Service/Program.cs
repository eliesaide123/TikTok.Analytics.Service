using TikTok.Analytics.Application.Mapping;
using TikTok.Analytics.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// AutoMapper
builder.Services.AddAutoMapper(typeof(TikTokMappingProfile));

// Infrastructure (HTTP clients, BigQuery, OAuth, Quartz scheduler, config)
builder.Services.AddInfrastructure(builder.Configuration);

// MVC controllers — hosts the TikTok OAuth login/callback endpoints
builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();
