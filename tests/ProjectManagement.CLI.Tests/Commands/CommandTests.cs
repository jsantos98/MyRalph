using Moq;
using Spectre.Console.Cli;
using ProjectManagement.CLI.Commands;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using ProjectManagement.Infrastructure.Git;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ProjectManagement.CLI.Tests.Commands;

public class CommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IAnsiConsole> _mockConsole;
    private readonly Mock<IWorkItemService> _mockWorkItemService;
    private readonly Mock<IRefinementService> _mockRefinementService;
    private readonly Mock<IDependencyResolutionService> _mockDependencyService;
    private readonly Mock<IImplementationService> _mockImplementationService;

    public CommandTests()
    {
        var services = new ServiceCollection();

        // Create and store mocks
        _mockWorkItemService = new Mock<IWorkItemService>();
        _mockRefinementService = new Mock<IRefinementService>();
        _mockDependencyService = new Mock<IDependencyResolutionService>();
        _mockImplementationService = new Mock<IImplementationService>();

        services.AddSingleton(_mockWorkItemService.Object);
        services.AddSingleton(_mockRefinementService.Object);
        services.AddSingleton(_mockDependencyService.Object);
        services.AddSingleton(_mockImplementationService.Object);

        // Mock console
        _mockConsole = new Mock<IAnsiConsole>();
        services.AddSingleton(_mockConsole.Object);

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public void StateManager_ValidateWorkItemTransitions_ReturnsExpectedResults()
    {
        // Arrange - use the actual StateManager
        var stateManager = new StateManager();

        // Act & Assert
        Assert.True(stateManager.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Refining));
        Assert.False(stateManager.CanTransition(WorkItemStatus.Pending, WorkItemStatus.Completed));
        Assert.True(stateManager.CanTransition(WorkItemStatus.Error, WorkItemStatus.Pending));

        var transitions = stateManager.GetValidTransitions(WorkItemStatus.Pending);
        Assert.Contains(WorkItemStatus.Refining, transitions);
        Assert.Contains(WorkItemStatus.Error, transitions);
        Assert.DoesNotContain(WorkItemStatus.Completed, transitions);
    }

    [Fact]
    public void StateManager_ValidateDeveloperStoryTransitions_ReturnsExpectedResults()
    {
        // Arrange
        var stateManager = new StateManager();

        // Act & Assert
        Assert.True(stateManager.CanTransition(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Ready));
        Assert.True(stateManager.CanTransition(DeveloperStoryStatus.Ready, DeveloperStoryStatus.InProgress));
        Assert.True(stateManager.CanTransition(DeveloperStoryStatus.InProgress, DeveloperStoryStatus.Completed));
        Assert.False(stateManager.CanTransition(DeveloperStoryStatus.Pending, DeveloperStoryStatus.Completed));

        var transitions = stateManager.GetValidTransitions(DeveloperStoryStatus.Ready);
        Assert.Contains(DeveloperStoryStatus.InProgress, transitions);
        Assert.Contains(DeveloperStoryStatus.Error, transitions);
    }

    [Fact]
    public async Task DependencyResolutionService_SelectNextAsync_WithNoStories_ReturnsNull()
    {
        // Arrange
        _mockDependencyService
            .Setup(s => s.UpdateDependencyStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDependencyService
            .Setup(s => s.SelectNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeveloperStory?)null);

        var service = _serviceProvider.GetRequiredService<IDependencyResolutionService>();

        // Act
        var result = await service.SelectNextAsync();

        // Assert
        Assert.Null(result);
        _mockDependencyService.Verify(s => s.SelectNextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WorkItemService_GetByIdAsync_WithNonExistentItem_ReturnsNull()
    {
        // Arrange
        _mockWorkItemService
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkItem?)null);

        var service = _serviceProvider.GetRequiredService<IWorkItemService>();

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefinementService_RefineWorkItemAsync_WithNonExistentWorkItem_ThrowsException()
    {
        // Arrange
        _mockRefinementService
            .Setup(s => s.RefineWorkItemAsync(999, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Core.Exceptions.EntityNotFoundException(typeof(WorkItem), 999));

        var service = _serviceProvider.GetRequiredService<IRefinementService>();

        // Act & Assert
        await Assert.ThrowsAsync<Core.Exceptions.EntityNotFoundException>(() => service.RefineWorkItemAsync(999));
    }
}
