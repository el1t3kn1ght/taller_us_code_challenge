using Microsoft.AspNetCore.SignalR;
using real_time_task_management.Entities;

namespace real_time_task_management.Hub
{
    public class TaskHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ SignalR Client connected: {Context.ConnectionId}");
            Console.WriteLine($"   User Agent: {Context.GetHttpContext()?.Request.Headers["User-Agent"]}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"❌ SignalR Client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                Console.WriteLine($"   Error: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task NotifyTaskAdded(TaskItem task)
        {
            await Clients.All.SendAsync("TaskAdded", task);
        }

        public async Task NotifyTaskUpdated(TaskItem task)
        {
            await Clients.All.SendAsync("TaskUpdated", task);
        }

        public async Task NotifyTaskDeleted(int taskId)
        {
            await Clients.All.SendAsync("TaskDeleted", taskId);
        }
    }
}