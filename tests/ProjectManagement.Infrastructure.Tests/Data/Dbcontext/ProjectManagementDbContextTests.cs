using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Dbcontext;

public class ProjectManagementDbContextTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;

    public ProjectManagementDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Constructor_WithValidOptions_InitializesContext()
    {
        // Assert
        Assert.NotNull(_context);
        Assert.NotNull(_context.WorkItems);
        Assert.NotNull(_context.DeveloperStories);
        Assert.NotNull(_context.DeveloperStoryDependencies);
        Assert.NotNull(_context.ExecutionLogs);
    }

    [Fact]
    public void WorkItems_DbSet_IsAccessible()
    {
        // Assert
        Assert.NotNull(_context.WorkItems);
        Assert.IsAssignableFrom<DbSet<WorkItem>>(_context.WorkItems);
    }

    [Fact]
    public void DeveloperStories_DbSet_IsAccessible()
    {
        // Assert
        Assert.NotNull(_context.DeveloperStories);
        Assert.IsAssignableFrom<DbSet<DeveloperStory>>(_context.DeveloperStories);
    }

    [Fact]
    public void DeveloperStoryDependencies_DbSet_IsAccessible()
    {
        // Assert
        Assert.NotNull(_context.DeveloperStoryDependencies);
        Assert.IsAssignableFrom<DbSet<DeveloperStoryDependency>>(_context.DeveloperStoryDependencies);
    }

    [Fact]
    public void ExecutionLogs_DbSet_IsAccessible()
    {
        // Assert
        Assert.NotNull(_context.ExecutionLogs);
        Assert.IsAssignableFrom<DbSet<ExecutionLog>>(_context.ExecutionLogs);
    }

    [Fact]
    public async Task OnModelCreating_WorkItem_Configuration_IsValid()
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description",
            Priority = 3,
            Status = WorkItemStatus.Pending
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Assert
        var entity = _context.Model.FindEntityType(typeof(WorkItem));
        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);
        Assert.Equal("Id", primaryKey.Properties[0].Name);

        var titleProperty = entity.FindProperty("Title");
        Assert.NotNull(titleProperty);
        Assert.Equal(500, titleProperty.GetMaxLength());

        var descriptionProperty = entity.FindProperty("Description");
        Assert.NotNull(descriptionProperty);
        Assert.Equal(4000, descriptionProperty.GetMaxLength());
    }

    [Fact]
    public async Task OnModelCreating_DeveloperStory_Configuration_IsValid()
    {
        // Arrange & Act
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        // Assert - verify the entity exists and has relationships configured
        var entity = _context.Model.FindEntityType(typeof(DeveloperStory));
        Assert.NotNull(entity);

        // Verify there are foreign keys defined
        var foreignKeys = entity.GetForeignKeys().ToList();
        Assert.NotEmpty(foreignKeys);
    }

    [Fact]
    public async Task OnModelCreating_DeveloperStoryDependency_Configuration_IsValid()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story1 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story 1",
            Description = "D1",
            Instructions = "I1"
        };
        var story2 = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.UnitTests,
            Title = "Story 2",
            Description = "D2",
            Instructions = "I2"
        };
        _context.DeveloperStories.AddRange(new[] { story1, story2 });
        await _context.SaveChangesAsync();

        var dep = new DeveloperStoryDependency
        {
            DependentStoryId = story2.Id,
            RequiredStoryId = story1.Id
        };
        _context.DeveloperStoryDependencies.Add(dep);
        await _context.SaveChangesAsync();

        // Assert - verify the entity exists and has relationships configured
        var entity = _context.Model.FindEntityType(typeof(DeveloperStoryDependency));
        Assert.NotNull(entity);

        // Verify there are foreign keys defined
        var foreignKeys = entity.GetForeignKeys().ToList();
        Assert.NotEmpty(foreignKeys);
    }

    [Fact]
    public async Task OnModelCreating_ExecutionLog_Configuration_IsValid()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);
        await _context.SaveChangesAsync();

        var log = new ExecutionLog
        {
            DeveloperStoryId = story.Id,
            EventType = ExecutionEventType.Started,
            Details = "Test"
        };
        _context.ExecutionLogs.Add(log);
        await _context.SaveChangesAsync();

        // Assert - verify the entity exists and has relationships configured
        var entity = _context.Model.FindEntityType(typeof(ExecutionLog));
        Assert.NotNull(entity);

        // Verify there are foreign keys defined
        var foreignKeys = entity.GetForeignKeys().ToList();
        Assert.NotEmpty(foreignKeys);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_SavesAllChanges()
    {
        // Arrange
        var workItem1 = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI1",
            Description = "D1"
        };
        var workItem2 = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "B1",
            Description = "D2"
        };

        // Act
        _context.WorkItems.AddRange(workItem1, workItem2);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.WorkItems.ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task ChangeTracker_WithModifiedEntity_TracksChanges()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Original",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        workItem.Title = "Modified";
        var entry = _context.Entry(workItem);

        // Assert
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal("Modified", entry.Entity.Title);
        Assert.Equal("Original", entry.OriginalValues["Title"]!);
    }

    [Fact]
    public async Task ChangeTracker_WithDeletedEntity_TracksDeletion()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();

        // Act
        _context.WorkItems.Remove(workItem);
        var entry = _context.Entry(workItem);

        // Assert
        Assert.Equal(EntityState.Deleted, entry.State);
    }

    [Fact]
    public void Model_EnsureAllEntitiesAreConfigured()
    {
        // Assert
        var model = _context.Model;
        Assert.NotNull(model);

        var workItemEntity = model.FindEntityType(typeof(WorkItem));
        Assert.NotNull(workItemEntity);

        var developerStoryEntity = model.FindEntityType(typeof(DeveloperStory));
        Assert.NotNull(developerStoryEntity);

        var dependencyEntity = model.FindEntityType(typeof(DeveloperStoryDependency));
        Assert.NotNull(dependencyEntity);

        var executionLogEntity = model.FindEntityType(typeof(ExecutionLog));
        Assert.NotNull(executionLogEntity);
    }
}
