using AutoMapper;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Common.Mappings
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
