using real_time_task_management.Entities;

namespace real_time_task_management.SummarizationServices
{
    public interface ISummarizationService
    {
        Task<string> SummarizeTasksAsync(List<TaskItem> taskItems);
    }
}
