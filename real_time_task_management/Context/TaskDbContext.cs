using Microsoft.EntityFrameworkCore;
using real_time_task_management.Entities;

namespace real_time_task_management.Context
{
    public class TaskDbContext(DbContextOptions<TaskDbContext> opt) : DbContext(opt)
    {
        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsCompleted).IsRequired();
            });
        }
    }
}
