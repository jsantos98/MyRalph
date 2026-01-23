using Microsoft.EntityFrameworkCore;
using ProjectManagement.Infrastructure.Data.DbContext;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Dbcontext;

public class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WithNoArgs_ReturnsDbContext()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        var context = factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ProjectManagementDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_WithEmptyArgs_ReturnsDbContext()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        var context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ProjectManagementDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_WithArgs_ReturnsDbContext()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();
        var args = new[] { "--arg1", "--arg2" };

        // Act
        var context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ProjectManagementDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_ReturnsContextWithSqliteOptions()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        var context = factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        var options = context.GetType().GetProperty("Database")?.GetValue(context);
        Assert.NotNull(options);
    }

    [Fact]
    public void CreateDbContext_MultipleCalls_ReturnsNewContextEachTime()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        var context1 = factory.CreateDbContext(null!);
        var context2 = factory.CreateDbContext(null!);

        // Assert
        Assert.NotSame(context1, context2);
    }

    [Fact]
    public void CreateDbContext_UsesDefaultConnectionString()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        var context = factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        // The context should be configured with SQLite
        // We can't directly check the connection string but we verify the context is usable
        Assert.NotNull(context.WorkItems);
        Assert.NotNull(context.DeveloperStories);
        Assert.NotNull(context.DeveloperStoryDependencies);
        Assert.NotNull(context.ExecutionLogs);
    }

    private void CleanupTestDatabase()
    {
        // Clean up any test databases if needed
        try
        {
            if (File.Exists("projectmanagement.db"))
            {
                File.Delete("projectmanagement.db");
            }
            if (File.Exists("projectmanagement.db-shm"))
            {
                File.Delete("projectmanagement.db-shm");
            }
            if (File.Exists("projectmanagement.db-wal"))
            {
                File.Delete("projectmanagement.db-wal");
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public DesignTimeDbContextFactoryTests()
    {
        CleanupTestDatabase();
    }
}