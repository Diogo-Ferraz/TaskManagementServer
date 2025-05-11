using AutoMapper;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Mappings
{
    public class ProjectMappingProfile : Profile
    {
        public ProjectMappingProfile()
        {
            CreateMap<Project, ProjectDto>()
                .ForMember(dest => dest.TaskItems, opt => opt.MapFrom(src => src.TaskItems));

            CreateMap<CreateProjectCommand, Project>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.OwnerUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.Members, opt => opt.Ignore())
                 .ForMember(dest => dest.TaskItems, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());

            CreateMap<UpdateProjectCommand, Project>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.OwnerUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.Members, opt => opt.Ignore())
                 .ForMember(dest => dest.TaskItems, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());
        }
    }
}
