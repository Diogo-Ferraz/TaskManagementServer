using AutoMapper;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.TaskItems.Mappings
{
    public class TaskItemMappingProfile : Profile
    {
        public TaskItemMappingProfile()
        {
            CreateMap<TaskItem, TaskItemDto>();

            CreateMap<CreateTaskItemCommand, TaskItem>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.Project, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());


            CreateMap<UpdateTaskItemCommand, TaskItem>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                 .ForMember(dest => dest.Project, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());
        }
    }
}
