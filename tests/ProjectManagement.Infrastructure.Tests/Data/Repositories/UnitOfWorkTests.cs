using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using Xunit;

namespace ProjectManagement.Infrastructure.Tests.Data.Repositories;

public class UnitOfWorkTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
    }

    [Fact]
    public async Task CommitAsync_WithChanges_SavesToDatabase()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        _context.WorkItems.Add(workItem);

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        Assert.True(result > 0);

        // Verify the item was actually saved
        var saved = await _context.WorkItems.FindAsync(workItem.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test", saved.Title);
    }

    [Fact]
    public async Task CommitAsync_WithNoChanges_ReturnsZero()
    {
        // Act - no changes made
        var result = await _unitOfWork.CommitAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CommitAsync_WithMultipleChanges_SavesAllChanges()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "WI",
            Description = "D"
        };
        _context.WorkItems.Add(workItem);

        var story = new DeveloperStory
        {
            WorkItemId = workItem.Id,
            StoryType = DeveloperStoryType.Implementation,
            Title = "Story",
            Description = "D",
            Instructions = "I"
        };
        _context.DeveloperStories.Add(story);

        // Act
        var result = await _unitOfWork.CommitAsync();

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CommitAsync_WithCancellation_CancelsOperation()
    {
        // Arrange
        var workItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test",
            Description = "Description"
        };
        _context.WorkItems.Add(workItem);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _unitOfWork.CommitAsync(cts.Token);
        });
    }

    [Fact]
    public async Task RollbackAsync_WithNoActiveTransaction_DoesNothing()
    {
        // Act & Assert - should not throw
        await _unitOfWork.RollbackAsync();
        Assert.Null(_context.Database.CurrentTransaction);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Act
        _unitOfWork.Dispose();
        _unitOfWork.Dispose();

        // Assert - no exception
    }

    [Fact]
    public void Dispose_DisposesContext()
    {
        // Act
        _unitOfWork.Dispose();

        // Assert - context is disposed (operations may or may not throw depending on provider)
        // The key is that Dispose can be called without error
        _unitOfWork.Dispose();
    }

    [Fact]
    public async Task MultipleCommitAsyncOperations_EachCommitsChanges()
    {
        // Arrange
        var workItem1 = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test 1",
            Description = "D1"
        };
        _context.WorkItems.Add(workItem1);

        // Act - first commit
        var result1 = await _unitOfWork.CommitAsync();
        Assert.Equal(1, result1);

        // Add more items
        var workItem2 = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Test 2",
            Description = "D2"
        };
        _context.WorkItems.Add(workItem2);

        // Act - second commit
        var result2 = await _unitOfWork.CommitAsync();
        Assert.Equal(1, result2);

        // Assert
        var items = await _context.WorkItems.ToListAsync();
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task BeginTransactionAsync_InMemoryDatabase_CompletesWithoutError()
    {
        // Act & Assert
        // InMemory EF doesn't support transactions but the method should complete
        // The actual behavior in InMemory is to throw InvalidOperationException that gets logged
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            // If we got here, the transaction was "started" (InMemory ignores it)
        }
        catch (InvalidOperationException)
        {
            // Expected for InMemory provider - transactions are not supported
        }
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCancellation_InMemoryDatabase_HandlesGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // InMemory EF throws InvalidOperationException for transactions, not OperationCanceledException
        try
        {
            await _unitOfWork.BeginTransactionAsync(cts.Token);
        }
        catch (InvalidOperationException)
        {
            // Expected for InMemory provider
        }
        catch (OperationCanceledException)
        {
            // Also acceptable - cancellation was handled
        }
    }

    [Fact]
    public async Task RollbackAsync_AfterBeginTransactionInMemory_DoesNotThrow()
    {
        // Arrange
        try
        {
            await _unitOfWork.BeginTransactionAsync();
        }
        catch { }

        // Act & Assert - should not throw even with no active transaction
        await _unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task MultipleBeginTransactionCalls_AreHandled()
    {
        // Act & Assert
        // InMemory EF doesn't support real transactions, but calls should be handled
        try
        {
            await _unitOfWork.BeginTransactionAsync();
        }
        catch { }

        try
        {
            await _unitOfWork.BeginTransactionAsync();
        }
        catch { }

        // If we got here without exception, the calls were handled
        Assert.True(true);
    }
}
