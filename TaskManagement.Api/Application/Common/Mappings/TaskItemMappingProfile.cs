using AutoMapper;
using TaskManagement.Api.Application.TaskItems.Commands;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Common.Mappings
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
