using AutoMapper;
using TaskManagement.Api.Features.Tasks.Commands;
using TaskManagement.Api.Features.Tasks.Models;
using TaskManagement.Api.Features.Tasks.Models.DTOs;

namespace TaskManagement.Api.Features.Tasks.Mappings
{
    public class TaskItemMappingProfile : Profile
    {
        public TaskItemMappingProfile()
        {
            CreateMap<TaskItem, TaskItemDto>()
                .ForMember(
                    dest => dest.ProjectName,
                    opt => opt.MapFrom(src => src.Project.Name))
                .ForMember(
                    dest => dest.AssignedUserName,
                    opt => opt.MapFrom(src => src.AssignedUser.UserName));

            CreateMap<CreateTaskItemCommand, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));

            CreateMap<UpdateTaskItemCommand, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore());
        }
    }
}
