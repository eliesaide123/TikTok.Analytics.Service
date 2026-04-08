using AutoMapper;
using TikTok.Analytics.Application.DTOs.Display;
using TikTok.Analytics.Application.DTOs.Business;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Application.Mapping;

public class TikTokMappingProfile : Profile
{
    public TikTokMappingProfile()
    {
        // Display API -> Domain
        CreateMap<UserInfoDto, UserProfile>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.SnapshotDate, opt => opt.MapFrom(_ => DateTime.UtcNow.Date))
            .ForMember(d => d.IngestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<VideoDto, Video>()
            .ForMember(d => d.CreateTime, opt => opt.MapFrom(s => DateTimeOffset.FromUnixTimeSeconds(s.CreateTime).UtcDateTime))
            .ForMember(d => d.SnapshotDate, opt => opt.MapFrom(_ => DateTime.UtcNow.Date))
            .ForMember(d => d.IngestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Business API -> Domain
        CreateMap<AccountMetricsDto, AccountMetrics>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.IngestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<FollowerGrowthDto, FollowerGrowth>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.IngestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<BusinessVideoDto, VideoAnalytics>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.VideoId, opt => opt.MapFrom(s => s.ItemId))
            .ForMember(d => d.SnapshotDate, opt => opt.MapFrom(_ => DateTime.UtcNow.Date))
            .ForMember(d => d.IngestedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}
