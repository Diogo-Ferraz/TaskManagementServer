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
            CreateMap<TaskItem, TaskItemDto>()
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : string.Empty)); //TODO: check if this works

            CreateMap<CreateTaskItemCommand, TaskItem>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.Project, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserName, opt => opt.Ignore());


            CreateMap<UpdateTaskItemCommand, TaskItem>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                 .ForMember(dest => dest.Project, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserName, opt => opt.Ignore());

            CreateMap<PatchTaskItemCommand, TaskItem>()
                 .ForMember(dest => dest.Title, opt =>
                 {
                     opt.PreCondition(src => src.Title.HasValue);
                     opt.MapFrom(src => src.Title.Value!);
                 })
                 .ForMember(dest => dest.Description, opt =>
                 {
                     opt.PreCondition(src => src.Description.HasValue);
                     opt.MapFrom(src => src.Description.Value);
                 })
                 .ForMember(dest => dest.Status, opt =>
                 {
                     opt.PreCondition(src => src.Status.HasValue);
                     opt.MapFrom(src => src.Status.Value);
                 })
                 .ForMember(dest => dest.DueDate, opt =>
                 {
                     opt.PreCondition(src => src.DueDate.HasValue);
                     opt.MapFrom(src => src.DueDate.Value);
                 })
                 .ForMember(dest => dest.AssignedUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                 .ForMember(dest => dest.Project, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.CreatedByUserName, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore())
                 .ForMember(dest => dest.LastModifiedByUserName, opt => opt.Ignore());
        }
    }
}
