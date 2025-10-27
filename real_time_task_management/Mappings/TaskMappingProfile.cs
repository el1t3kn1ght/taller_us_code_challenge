using AutoMapper;
using real_time_task_management.Dtos;
using real_time_task_management.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace real_time_task_management.Mappings;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        // Entity to DTO
        CreateMap<TaskItem, TaskResponseDto>();
        CreateMap<TaskSummary, TaskSummaryDto>();

        // DTO to Entity
        CreateMap<CreateTaskDto, TaskItem>();
        CreateMap<UpdateTaskDto, TaskItem>();
    }
}