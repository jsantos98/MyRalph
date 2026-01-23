using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data;
using ProjectManagement.Infrastructure.Data.DbContext;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Database;

public class DatabaseInitializerTests : IAsyncLifetime
{
    private readonly string _databasePath;
    private readonly DbContextOptions<ProjectManagementDbContext> _options;

    public DatabaseInitializerTests()
    {
        // Use a unique database file for each test
        _databasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

        _options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
    }

    public async Task InitializeAsync()
    {
        // This runs before each test - ensure database doesn't exist
        if (File.Exists(_databasePath))
        {
            await Task.Run(() => File.Delete(_databasePath));
        }
    }

    public async Task DisposeAsync()
    {
        // This runs after each test - clean up the test database file
        await Task.Run(() =>
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
                // Ignore disposal errors during cleanup
            }
        });
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseDoesNotExist_CreatesDatabaseAndReturnsTrue()
    {
        // Arrange
        await using var context = new ProjectManagementDbContext(_options);
        var initializer = new DatabaseInitializer(context);

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        Assert.True(result, "InitializeAsync should return true when database is created");

        // Verify database was created by checking if we can connect and query
        await using var verifyContext = new ProjectManagementDbContext(_options);
        var canConnect = await verifyContext.Database.CanConnectAsync();
        Assert.True(canConnect, "Database should be created and connectable");

        // Verify tables exist by checking if we can query them (this will throw if tables don't exist)
        var workItemsCount = await verifyContext.WorkItems.CountAsync();
        Assert.Equal(0, workItemsCount); // Should be empty but table exists
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseExistsWithMigrations_DoesNotRecreateAndReturnsFalse()
    {
        // Arrange - Create the database using migrations first
        await using var createContext = new ProjectManagementDbContext(_options);
        await createContext.Database.MigrateAsync();

        // Add a test record to verify we're not re-creating
        var workItem = new WorkItem
        {
            Type = Core.Enums.WorkItemType.UserStory,
            Title = "Test Item",
            Description = "Test Description",
            Priority = 5,
            Status = Core.Enums.WorkItemStatus.Pending
        };
        // Set Id directly since it's a property we can access
        var idProperty = typeof(WorkItem).GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(workItem, 1);
        }
        createContext.WorkItems.Add(workItem);
        await createContext.SaveChangesAsync();

        var initializer = new DatabaseInitializer(createContext);

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        Assert.False(result, "InitializeAsync should return false when database already exists");

        // Verify the existing data is still there (we didn't re-create)
        await using var verifyContext = new ProjectManagementDbContext(_options);
        var items = await verifyContext.WorkItems.ToListAsync();
        Assert.Single(items);
        Assert.Equal("Test Item", items[0].Title);
    }

    [Fact]
    public async Task InitializeAsync_CanBeCalledMultipleTimesSafely()
    {
        // Arrange
        await using var context = new ProjectManagementDbContext(_options);
        var initializer = new DatabaseInitializer(context);

        // Act - Call multiple times
        var result1 = await initializer.InitializeAsync();
        var result2 = await initializer.InitializeAsync();
        var result3 = await initializer.InitializeAsync();

        // Assert
        Assert.True(result1, "First call should create the database");
        Assert.False(result2, "Second call should not create the database");
        Assert.False(result3, "Third call should not create the database");

        // Verify database is still functional
        await using var verifyContext = new ProjectManagementDbContext(_options);
        var canConnect = await verifyContext.Database.CanConnectAsync();
        Assert.True(canConnect, "Database should still be connectable after multiple initializations");
    }

    [Fact]
    public async Task InitializeAsync_CreatesAllRequiredTables()
    {
        // Arrange
        await using var context = new ProjectManagementDbContext(_options);
        var initializer = new DatabaseInitializer(context);

        // Act
        await initializer.InitializeAsync();

        // Assert - Verify all required tables exist
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        await connection.CloseAsync();

        // Verify all required tables are present
        Assert.Contains("DeveloperStoryDependencies", tables);
        Assert.Contains("DeveloperStories", tables);
        Assert.Contains("ExecutionLogs", tables);
        Assert.Contains("WorkItems", tables);
        Assert.Contains("__EFMigrationsHistory", tables);

        // Should have at least 5 tables (4 main tables + __EFMigrationsHistory)
        Assert.True(tables.Count >= 5, $"Expected at least 5 tables, but found {tables.Count}: {string.Join(", ", tables)}");
    }

    [Fact]
    public async Task InitializeAsync_WhenDatabaseIsCorruptOrInvalid_RecreatesDatabase()
    {
        // Arrange - Create an invalid database file
        await File.WriteAllTextAsync(_databasePath, "invalid database content", CancellationToken.None);

        await using var context = new ProjectManagementDbContext(_options);
        var initializer = new DatabaseInitializer(context);

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        Assert.True(result, "InitializeAsync should recreate the database when it's invalid");

        // Verify database is now valid and functional
        await using var verifyContext = new ProjectManagementDbContext(_options);
        var canConnect = await verifyContext.Database.CanConnectAsync();
        Assert.True(canConnect, "Database should be valid after recreation");

        // Verify we can query tables
        var workItemsCount = await verifyContext.WorkItems.CountAsync();
        Assert.Equal(0, workItemsCount);
    }
}
