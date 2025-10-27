using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using real_time_task_management.Context;
using real_time_task_management.Dtos;
using real_time_task_management.Entities;
using real_time_task_management.Hub;
using real_time_task_management.Mappings;
using real_time_task_management.Middleware;
using real_time_task_management.Repositories;
using real_time_task_management.SummarizationServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR();

// AutoMapper
builder.Services.AddAutoMapper(x =>
{
    x.AddProfile<TaskMappingProfile>();
});

// Repository Pattern
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Services
builder.Services.AddScoped<ISummarizationService, SummarizationService>();

// Database
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite("Data Source=tasks.db"));

builder.Services.AddHttpClient();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// 1. Global Exception Handler (should be first)
app.UseGlobalExceptionHandler();

// 2. CORS
app.UseCors("AllowAll");

// 3. HTTPS redirection
app.UseHttpsRedirection();

// 4. Map SignalR hub
app.MapHub<TaskHub>("/taskHub");

// API Endpoints
app.MapGet("/api/tasks", async (
    ITaskRepository repository,
    IMapper mapper) =>
{
    var tasks = await repository.GetAllTasksAsync();
    var tasksDto = mapper.Map<IEnumerable<TaskResponseDto>>(tasks);
    return Results.Ok(tasksDto);
})
.WithName("GetAllTasks")
.WithOpenApi();

app.MapGet("/api/tasks/{id}", async (
    int id,
    ITaskRepository repository,
    IMapper mapper) =>
{
    var task = await repository.GetTaskByIdAsync(id);

    if (task is null)
        return Results.NotFound(new { Message = $"Task with ID {id} not found." });

    var taskDto = mapper.Map<TaskResponseDto>(task);
    return Results.Ok(taskDto);
})
.WithName("GetTaskById")
.WithOpenApi();

app.MapPost("/api/tasks", async (
    CreateTaskDto createTaskDto,
    ITaskRepository repository,
    IMapper mapper,
    IHubContext<TaskHub> hubContext) =>
{
    var task = mapper.Map<TaskItem>(createTaskDto);
    var createdTask = await repository.CreateTaskAsync(task);

    var taskDto = mapper.Map<TaskResponseDto>(createdTask);

    Console.WriteLine($"📤 Broadcasting TaskAdded: {createdTask.Id} - {createdTask.Title}");
    await hubContext.Clients.All.SendAsync("TaskAdded", taskDto);

    return Results.Created($"/api/tasks/{createdTask.Id}", taskDto);
})
.WithName("CreateTask")
.WithOpenApi();

app.MapPut("/api/tasks/{id}", async (
    int id,
    UpdateTaskDto updateTaskDto,
    ITaskRepository repository,
    IMapper mapper,
    IHubContext<TaskHub> hubContext) =>
{
    var taskToUpdate = mapper.Map<TaskItem>(updateTaskDto);
    var updatedTask = await repository.UpdateTaskAsync(id, taskToUpdate);

    if (updatedTask is null)
        return Results.NotFound(new { Message = $"Task with ID {id} not found." });

    var taskDto = mapper.Map<TaskResponseDto>(updatedTask);

    Console.WriteLine($"📤 Broadcasting TaskUpdated: {updatedTask.Id} - {updatedTask.Title}");
    await hubContext.Clients.All.SendAsync("TaskUpdated", taskDto);

    return Results.Ok(taskDto);
})
.WithName("UpdateTask")
.WithOpenApi();

app.MapDelete("/api/tasks/{id}", async (
    int id,
    ITaskRepository repository,
    IHubContext<TaskHub> hubContext) =>
{
    var deleted = await repository.DeleteTaskAsync(id);

    if (!deleted)
        return Results.NotFound(new { Message = $"Task with ID {id} not found." });

    Console.WriteLine($"📤 Broadcasting TaskDeleted: {id}");
    await hubContext.Clients.All.SendAsync("TaskDeleted", id);

    return Results.NoContent();
})
.WithName("DeleteTask")
.WithOpenApi();

app.MapPost("/api/tasks/summary", async (
    ITaskRepository repository,
    ISummarizationService aiService,
    IMapper mapper) =>
{
    var tasks = await repository.GetAllTasksAsync();
    var aiSummary = await aiService.SummarizeTasksAsync([.. tasks]);

    var summary = new TaskSummary
    {
        TotalTasks = await repository.GetTotalTasksCountAsync(),
        CompletedTasks = await repository.GetCompletedTasksCountAsync(),
        PendingTasks = await repository.GetPendingTasksCountAsync(),
        AiSummary = aiSummary
    };

    var summaryDto = mapper.Map<TaskSummaryDto>(summary);
    return Results.Ok(summaryDto);
})
.WithName("GetTaskSummary")
.WithOpenApi();

Console.WriteLine("🚀 Task Management API is running!");
Console.WriteLine("📍 API: https://localhost:7131");
Console.WriteLine("📍 Swagger: https://localhost:7131/swagger");
Console.WriteLine("📍 SignalR Hub: https://localhost:7131/taskHub");

app.Run();