using AutoMapper;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Mappings
{
    public class ProjectMappingProfile : Profile
    {
        public ProjectMappingProfile()
        {
            CreateMap<Project, ProjectDto>()
                .ForMember(
                    dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User.UserName));

            CreateMap<TaskItem, TaskItemDto>()
                .ForMember(
                    dest => dest.AssignedUserName,
                    opt => opt.MapFrom(src => src.AssignedUser.UserName));

            CreateMap<CreateProjectCommand, Project>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));

            CreateMap<UpdateProjectCommand, Project>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
