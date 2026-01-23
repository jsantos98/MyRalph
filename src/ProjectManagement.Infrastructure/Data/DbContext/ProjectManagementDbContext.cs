using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;

namespace ProjectManagement.Infrastructure.Data.DbContext;

/// <summary>
/// Entity Framework Core DbContext for the Project Management system
/// </summary>
public class ProjectManagementDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// WorkItems table
    /// </summary>
    public DbSet<WorkItem> WorkItems { get; set; } = null!;

    /// <summary>
    /// DeveloperStories table
    /// </summary>
    public DbSet<DeveloperStory> DeveloperStories { get; set; } = null!;

    /// <summary>
    /// DeveloperStoryDependencies table
    /// </summary>
    public DbSet<DeveloperStoryDependency> DeveloperStoryDependencies { get; set; } = null!;

    /// <summary>
    /// ExecutionLogs table
    /// </summary>
    public DbSet<ExecutionLog> ExecutionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkItem configuration
        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.AcceptanceCriteria)
                .HasMaxLength(2000);

            entity.Property(e => e.Priority)
                .IsRequired()
                .HasDefaultValue(5);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Priority);
        });

        // DeveloperStory configuration
        modelBuilder.Entity<DeveloperStory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.WorkItem)
                .WithMany(w => w.DeveloperStories)
                .HasForeignKey(e => e.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.Instructions)
                .IsRequired()
                .HasMaxLength(8000);

            entity.Property(e => e.Priority)
                .IsRequired()
                .HasDefaultValue(5);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.StoryType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.GitBranch)
                .HasMaxLength(500);

            entity.Property(e => e.GitWorktree)
                .HasMaxLength(1000);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.Metadata)
                .HasMaxLength(4000);

            entity.HasIndex(e => e.WorkItemId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StoryType);
            entity.HasIndex(e => e.Priority);
        });

        // DeveloperStoryDependency configuration
        modelBuilder.Entity<DeveloperStoryDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.DependentStory)
                .WithMany(ds => ds.DependentStories)
                .HasForeignKey(e => e.DependentStoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RequiredStory)
                .WithMany(ds => ds.Dependencies)
                .HasForeignKey(e => e.RequiredStoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on (DependentStoryId, RequiredStoryId)
            entity.HasIndex(e => new { e.DependentStoryId, e.RequiredStoryId })
                .IsUnique();

            entity.HasIndex(e => e.DependentStoryId);
            entity.HasIndex(e => e.RequiredStoryId);
        });

        // ExecutionLog configuration
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.DeveloperStory)
                .WithMany(ds => ds.ExecutionLogs)
                .HasForeignKey(e => e.DeveloperStoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Details)
                .HasMaxLength(4000);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.Metadata)
                .HasMaxLength(4000);

            entity.HasIndex(e => e.DeveloperStoryId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
