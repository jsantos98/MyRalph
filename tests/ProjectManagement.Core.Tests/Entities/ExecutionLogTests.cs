using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using Xunit;

namespace ProjectManagement.Core.Tests.Entities;

public class ExecutionLogTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = ExecutionEventType.Started
        };

        // Assert
        Assert.Equal(1, log.DeveloperStoryId);
        Assert.Equal(ExecutionEventType.Started, log.EventType);
        Assert.Null(log.Details);
        Assert.Null(log.ErrorMessage);
        Assert.Null(log.Metadata);
        Assert.True(log.Timestamp <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(ExecutionEventType.Started)]
    [InlineData(ExecutionEventType.Completed)]
    [InlineData(ExecutionEventType.Failed)]
    [InlineData(ExecutionEventType.Retried)]
    [InlineData(ExecutionEventType.BranchCreated)]
    [InlineData(ExecutionEventType.WorktreeCreated)]
    [InlineData(ExecutionEventType.WorktreeRemoved)]
    [InlineData(ExecutionEventType.Info)]
    public void Constructor_WithAllEventTypes_SetsEventType(ExecutionEventType eventType)
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = eventType
        };

        // Assert
        Assert.Equal(eventType, log.EventType);
    }

    [Fact]
    public void Constructor_WithDetails_SetsDetails()
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = ExecutionEventType.Info,
            Details = "Processing step 1"
        };

        // Assert
        Assert.Equal("Processing step 1", log.Details);
    }

    [Fact]
    public void Constructor_WithError_SetsErrorMessage()
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = ExecutionEventType.Failed,
            ErrorMessage = "Connection timeout"
        };

        // Assert
        Assert.Equal("Connection timeout", log.ErrorMessage);
    }

    [Fact]
    public void Constructor_WithMetadata_SetsMetadata()
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = ExecutionEventType.Info,
            Metadata = "{\"attempt\":1,\"duration\":\"00:05:00\"}"
        };

        // Assert
        Assert.Equal("{\"attempt\":1,\"duration\":\"00:05:00\"}", log.Metadata);
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsAllProperties()
    {
        // Arrange & Act
        var log = new ExecutionLog
        {
            DeveloperStoryId = 1,
            EventType = ExecutionEventType.Completed,
            Details = "Implementation completed successfully",
            ErrorMessage = null,
            Metadata = "{\"lines\":100}"
        };

        // Assert
        Assert.Equal(1, log.DeveloperStoryId);
        Assert.Equal(ExecutionEventType.Completed, log.EventType);
        Assert.Equal("Implementation completed successfully", log.Details);
        Assert.Null(log.ErrorMessage);
        Assert.Equal("{\"lines\":100}", log.Metadata);
    }
}
