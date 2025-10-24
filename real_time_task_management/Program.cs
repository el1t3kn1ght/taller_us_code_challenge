using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using real_time_task_management.Context;
using real_time_task_management.Dtos;
using real_time_task_management.Entities;
using real_time_task_management.Hub;
using real_time_task_management.SummarizationServices;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddScoped<ISummarizationService, SummarizationService>();

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
            .AllowCredentials(); // Important for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ CRITICAL: Middleware order matters!
// 1. CORS must be first
app.UseCors("AllowAll");

// 2. HTTPS redirection
app.UseHttpsRedirection();

// 3. Map SignalR hub AFTER CORS
app.MapHub<TaskHub>("/taskHub");

// API Endpoints
app.MapGet("/api/tasks", async (TaskDbContext db) =>
{
    var tasks = await db.Tasks.OrderByDescending(t => t.CreatedAt).ToListAsync();
    return Results.Ok(tasks);
})
.WithName("GetAllTasks")
.WithOpenApi();

app.MapGet("/api/tasks/{id}", async (int id, TaskDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
})
.WithName("GetTaskById")
.WithOpenApi();

app.MapPost("/api/tasks", async (
    TaskItem task,
    TaskDbContext db,
    IHubContext<TaskHub> hubContext) =>
{
    task.CreatedAt = DateTime.UtcNow; // Ensure CreatedAt is set
    db.Tasks.Add(task);
    await db.SaveChangesAsync();

    Console.WriteLine($"📤 Broadcasting TaskAdded: {task.Id} - {task.Title}");
    await hubContext.Clients.All.SendAsync("TaskAdded", task);

    return Results.Created($"/api/tasks/{task.Id}", task);
})
.WithName("CreateTask")
.WithOpenApi();

app.MapPut("/api/tasks/{id}", async (
    int id,
    TaskItem updatedTask,
    TaskDbContext db,
    IHubContext<TaskHub> hubContext) =>
{
    var task = await db.Tasks.FindAsync(id);

    if (task is null)
        return Results.NotFound();

    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.IsCompleted = updatedTask.IsCompleted;

    await db.SaveChangesAsync();

    Console.WriteLine($"📤 Broadcasting TaskUpdated: {task.Id} - {task.Title}");
    await hubContext.Clients.All.SendAsync("TaskUpdated", task);

    return Results.Ok(task);
})
.WithName("UpdateTask")
.WithOpenApi();

app.MapDelete("/api/tasks/{id}", async (
    int id,
    TaskDbContext db,
    IHubContext<TaskHub> hubContext) =>
{
    var task = await db.Tasks.FindAsync(id);

    if (task is null)
        return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();

    Console.WriteLine($"📤 Broadcasting TaskDeleted: {id}");
    await hubContext.Clients.All.SendAsync("TaskDeleted", id);

    return Results.NoContent();
})
.WithName("DeleteTask")
.WithOpenApi();

app.MapPost("/api/tasks/summary", async (
    TaskDbContext db,
    ISummarizationService aiService) =>
{
    var tasks = await db.Tasks.ToListAsync();

    var aiSummary = await aiService.SummarizeTasksAsync(tasks);

    var summary = new TaskSummary
    {
        TotalTasks = tasks.Count,
        CompletedTasks = tasks.Count(t => t.IsCompleted),
        PendingTasks = tasks.Count(t => !t.IsCompleted),
        AiSummary = aiSummary
    };

    return Results.Ok(summary);
})
.WithName("GetTaskSummary")
.WithOpenApi();

Console.WriteLine("🚀 Task Management API is running!");
Console.WriteLine("📍 API: https://localhost:7131");
Console.WriteLine("📍 Swagger: https://localhost:7131/swagger");
Console.WriteLine("📍 SignalR Hub: https://localhost:7131/taskHub");

app.Run();