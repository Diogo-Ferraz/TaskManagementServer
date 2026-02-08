using AutoMapper;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Models.DTOs;

namespace TaskManagement.Api.Features.Activity.Mappings
{
    public class ActivityMappingProfile : Profile
    {
        public ActivityMappingProfile()
        {
            CreateMap<ActivityLog, ActivityLogDto>()
                .ForMember(dest => dest.ActorUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.CreatedByUserName))
                .ForMember(dest => dest.OccurredAt, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
}
