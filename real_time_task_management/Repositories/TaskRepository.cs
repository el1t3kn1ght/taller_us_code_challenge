using Microsoft.EntityFrameworkCore;
using real_time_task_management.Context;
using real_time_task_management.Entities;

namespace real_time_task_management.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;

    public TaskRepository(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> UpdateTaskAsync(int id, TaskItem updatedTask)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task is null)
            return null;

        task.Title = updatedTask.Title;
        task.Description = updatedTask.Description;
        task.IsCompleted = updatedTask.IsCompleted;

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task is null)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalTasksCountAsync()
    {
        return await _context.Tasks.CountAsync();
    }

    public async Task<int> GetCompletedTasksCountAsync()
    {
        return await _context.Tasks.CountAsync(t => t.IsCompleted);
    }

    public async Task<int> GetPendingTasksCountAsync()
    {
        return await _context.Tasks.CountAsync(t => !t.IsCompleted);
    }
}