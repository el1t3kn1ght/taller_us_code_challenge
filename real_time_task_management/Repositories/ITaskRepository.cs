using real_time_task_management.Entities;

namespace real_time_task_management.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllTasksAsync();
    Task<TaskItem?> GetTaskByIdAsync(int id);
    Task<TaskItem> CreateTaskAsync(TaskItem task);
    Task<TaskItem?> UpdateTaskAsync(int id, TaskItem task);
    Task<bool> DeleteTaskAsync(int id);
    Task<int> GetTotalTasksCountAsync();
    Task<int> GetCompletedTasksCountAsync();
    Task<int> GetPendingTasksCountAsync();
}