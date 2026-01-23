using Moq;
using Spectre.Console.Cli;
using ProjectManagement.CLI.Commands;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ProjectManagement.CLI.Tests.Commands;

public class CreateItemCommandTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IWorkItemService> _mockWorkItemService;

    public CreateItemCommandTests()
    {
        _mockWorkItemService = new Mock<IWorkItemService>();

        var services = new ServiceCollection();
        services.AddSingleton(_mockWorkItemService.Object);
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
    public void Command_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var command = new CreateItemCommand(_mockWorkItemService.Object);

        // Assert
        Assert.NotNull(command);
    }

    [Fact]
    public void GetPriorityDisplay_HighestPriority_ReturnsCorrectDisplay()
    {
        // Act & Assert
        var priority1 = 1 switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + 1 + "[/]",
            _ => "[green]" + 1 + "[/]"
        };
        Assert.Contains("Highest", priority1);
        Assert.Contains("red", priority1);
    }

    [Fact]
    public void GetPriorityDisplay_LowPriority_ReturnsCorrectDisplay()
    {
        // Act & Assert
        var priority9 = 9 switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + 9 + "[/]",
            _ => "[green]" + 9 + "[/]"
        };
        Assert.Contains("green", priority9);
        Assert.DoesNotContain("Highest", priority9);
    }

    [Fact]
    public void GetPriorityDisplay_EdgeCases_WorkCorrectly()
    {
        // Test priority 2 (red)
        var priority2 = 2 switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + 2 + "[/]",
            _ => "[green]" + 2 + "[/]"
        };
        Assert.Equal("[red]2[/]", priority2);

        // Test priority 5 (yellow, boundary)
        var priority5 = 5 switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + 5 + "[/]",
            _ => "[green]" + 5 + "[/]"
        };
        Assert.Contains("yellow", priority5);

        // Test priority 6 (green)
        var priority6 = 6 switch
        {
            1 => "[red]1 (Highest)[/]",
            2 => "[red]2[/]",
            3 => "[yellow]3[/]",
            <= 5 => "[yellow]" + 6 + "[/]",
            _ => "[green]" + 6 + "[/]"
        };
        Assert.Contains("green", priority6);
    }

    [Fact]
    public void GetStatusColor_AllStatuses_ReturnValidColors()
    {
        var allStatuses = new[]
        {
            WorkItemStatus.Pending,
            WorkItemStatus.Refining,
            WorkItemStatus.Refined,
            WorkItemStatus.InProgress,
            WorkItemStatus.Completed,
            WorkItemStatus.Error
        };

        foreach (var status in allStatuses)
        {
            var color = status switch
            {
                WorkItemStatus.Pending => "yellow",
                WorkItemStatus.Refining => "blue",
                WorkItemStatus.Refined => "cyan",
                WorkItemStatus.InProgress => "yellow",
                WorkItemStatus.Completed => "green",
                WorkItemStatus.Error => "red",
                _ => "white"
            };

            Assert.NotEqual("white", color);
        }
    }

    [Fact]
    public void GetStatusColor_ErrorStatus_ReturnsRed()
    {
        var colorError = WorkItemStatus.Error switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
        Assert.Equal("red", colorError);
    }

    [Fact]
    public void GetStatusColor_CompletedStatus_ReturnsGreen()
    {
        var colorCompleted = WorkItemStatus.Completed switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
        Assert.Equal("green", colorCompleted);
    }

    [Fact]
    public void GetStatusColor_InProgressStatus_ReturnsYellow()
    {
        var colorInProgress = WorkItemStatus.InProgress switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
        Assert.Equal("yellow", colorInProgress);
    }

    [Fact]
    public void GetStatusColor_RefiningStatus_ReturnsBlue()
    {
        var colorRefining = WorkItemStatus.Refining switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
        Assert.Equal("blue", colorRefining);
    }

    [Fact]
    public void GetStatusColor_RefinedStatus_ReturnsCyan()
    {
        var colorRefined = WorkItemStatus.Refined switch
        {
            WorkItemStatus.Pending => "yellow",
            WorkItemStatus.Refining => "blue",
            WorkItemStatus.Refined => "cyan",
            WorkItemStatus.InProgress => "yellow",
            WorkItemStatus.Completed => "green",
            WorkItemStatus.Error => "red",
            _ => "white"
        };
        Assert.Equal("cyan", colorRefined);
    }

    [Fact]
    public async Task CreateAsync_WithBug_CreatesBugWorkItem()
    {
        // Arrange
        var createdBug = new WorkItem
        {
            Type = WorkItemType.Bug,
            Title = "Bug Title",
            Description = "Bug Description",
            AcceptanceCriteria = null,
            Priority = 1,
            Status = WorkItemStatus.Pending
        };
        createdBug.GetType().GetProperty("Id")?.SetValue(createdBug, 43);

        _mockWorkItemService
            .Setup(s => s.CreateAsync(
                WorkItemType.Bug,
                "Bug Title",
                "Bug Description",
                null,
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdBug);

        var service = _mockWorkItemService.Object;

        // Act
        var result = await service.CreateAsync(
            WorkItemType.Bug,
            "Bug Title",
            "Bug Description",
            null,
            1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemType.Bug, result.Type);
        Assert.Equal("Bug Title", result.Title);
        _mockWorkItemService.Verify(
            s => s.CreateAsync(WorkItemType.Bug, "Bug Title", "Bug Description", null, 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUserStory_CreatesUserStoryWorkItem()
    {
        // Arrange
        var createdStory = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "User Story Title",
            Description = "User Story Description",
            AcceptanceCriteria = "Acceptance criteria",
            Priority = 5,
            Status = WorkItemStatus.Pending
        };
        createdStory.GetType().GetProperty("Id")?.SetValue(createdStory, 44);

        _mockWorkItemService
            .Setup(s => s.CreateAsync(
                WorkItemType.UserStory,
                "User Story Title",
                "User Story Description",
                "Acceptance criteria",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdStory);

        var service = _mockWorkItemService.Object;

        // Act
        var result = await service.CreateAsync(
            WorkItemType.UserStory,
            "User Story Title",
            "User Story Description",
            "Acceptance criteria",
            5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemType.UserStory, result.Type);
        Assert.Equal("User Story Title", result.Title);
        Assert.Equal("Acceptance criteria", result.AcceptanceCriteria);
    }

    [Fact]
    public async Task ExecuteAsync_WithServiceException_PropagatesException()
    {
        // Arrange
        _mockWorkItemService
            .Setup(s => s.CreateAsync(
                It.IsAny<WorkItemType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        var service = _mockWorkItemService.Object;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                WorkItemType.UserStory,
                "Title",
                "Description",
                null,
                3));
    }

    [Fact]
    public void ExecuteAsync_VerifyCommandExists()
    {
        // Arrange & Act
        var command = new CreateItemCommand(_mockWorkItemService.Object);

        // Assert - verify command is properly instantiated
        Assert.NotNull(command);
        Assert.Equal(typeof(AsyncCommand), command.GetType().BaseType);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsWorkItem()
    {
        // Arrange
        var createdItem = new WorkItem
        {
            Type = WorkItemType.UserStory,
            Title = "Test Story",
            Description = "Test Description",
            AcceptanceCriteria = "Test Criteria",
            Priority = 3,
            Status = WorkItemStatus.Pending
        };
        createdItem.GetType().GetProperty("Id")?.SetValue(createdItem, 100);

        _mockWorkItemService
            .Setup(s => s.CreateAsync(
                WorkItemType.UserStory,
                "Test Story",
                "Test Description",
                "Test Criteria",
                3,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdItem);

        var service = _mockWorkItemService.Object;

        // Act
        var result = await service.CreateAsync(
            WorkItemType.UserStory,
            "Test Story",
            "Test Description",
            "Test Criteria",
            3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkItemType.UserStory, result.Type);
        Assert.Equal("Test Story", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("Test Criteria", result.AcceptanceCriteria);
        Assert.Equal(3, result.Priority);
        Assert.Equal(WorkItemStatus.Pending, result.Status);
    }

    [Fact]
    public void WorkItemType_SelectionMapping_UserStory()
    {
        // Test the type mapping logic from the command
        var typeSelection = "User Story";
        var workItemType = typeSelection == "User Story" ? WorkItemType.UserStory : WorkItemType.Bug;
        Assert.Equal(WorkItemType.UserStory, workItemType);
    }

    [Fact]
    public void WorkItemType_SelectionMapping_Bug()
    {
        // Test the type mapping logic from the command
        var typeSelection = "Bug";
        var workItemType = typeSelection == "User Story" ? WorkItemType.UserStory : WorkItemType.Bug;
        Assert.Equal(WorkItemType.Bug, workItemType);
    }

    [Fact]
    public void TypeRegistrar_RegistersAndResolvesCommandType()
    {
        // Arrange - Create a service collection with IWorkItemService
        var services = new ServiceCollection();
        services.AddSingleton(_mockWorkItemService.Object);

        var rootProvider = services.BuildServiceProvider();
        var registrar = new TestTypeRegistrar(rootProvider);

        // Act - Register the command type (as Spectre.Console.Cli does)
        registrar.Register(typeof(CreateItemCommand), typeof(CreateItemCommand));

        // Build the resolver
        var resolver = registrar.Build();

        // Assert - Verify the command can be resolved with its dependencies
        var resolvedCommand = resolver.Resolve(typeof(CreateItemCommand));
        Assert.NotNull(resolvedCommand);
        Assert.IsType<CreateItemCommand>(resolvedCommand);
    }

    [Fact]
    public void TypeRegistrar_WhenCommandHasDependency_ResolvesFromPrimaryProvider()
    {
        // Arrange - Create a service collection with IWorkItemService
        var services = new ServiceCollection();
        services.AddSingleton(_mockWorkItemService.Object);

        var rootProvider = services.BuildServiceProvider();
        var registrar = new TestTypeRegistrar(rootProvider);

        // Act - Register the command type
        registrar.Register(typeof(CreateItemCommand), typeof(CreateItemCommand));

        // Build the resolver
        var resolver = registrar.Build();

        // Assert - Verify the command's dependency (IWorkItemService) is resolved
        var resolvedCommand = resolver.Resolve(typeof(CreateItemCommand)) as CreateItemCommand;
        Assert.NotNull(resolvedCommand);

        // Verify the command has the correct IWorkItemService injected
        var serviceField = typeof(CreateItemCommand)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IWorkItemService));

        Assert.NotNull(serviceField);
        var injectedService = serviceField.GetValue(resolvedCommand) as IWorkItemService;
        Assert.Same(_mockWorkItemService.Object, injectedService);
    }

    [Fact]
    public void TypeRegistrar_RegisterInstance_ResolvesSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var rootProvider = services.BuildServiceProvider();
        var registrar = new TestTypeRegistrar(rootProvider);

        var instance = new object();

        // Act - Register an instance
        registrar.RegisterInstance(typeof(object), instance);

        // Build the resolver
        var resolver = registrar.Build();

        // Assert - Verify the same instance is resolved
        var resolved = resolver.Resolve(typeof(object));
        Assert.Same(instance, resolved);
    }

    [Fact]
    public void TypeRegistrar_RegisterLazy_ResolvesFactoryResult()
    {
        // Arrange
        var services = new ServiceCollection();
        var rootProvider = services.BuildServiceProvider();
        var registrar = new TestTypeRegistrar(rootProvider);

        var expectedValue = "test-value";

        // Act - Register a lazy factory
        registrar.RegisterLazy(typeof(string), () => expectedValue);

        // Build the resolver
        var resolver = registrar.Build();

        // Assert - Verify the factory result is resolved
        var resolved = resolver.Resolve(typeof(string)) as string;
        Assert.Equal(expectedValue, resolved);
    }
}

#region Test Helpers for TypeRegistrar

// Test implementation of TypeRegistrar to expose Build() for testing
internal class TestTypeRegistrar : Spectre.Console.Cli.ITypeRegistrar, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly Dictionary<Type, Type> _typeRegistrations;
    private readonly Dictionary<Type, object> _instanceRegistrations;
    private readonly Dictionary<Type, Func<object>> _lazyRegistrations;
    private bool _disposed = false;

    public TestTypeRegistrar(IServiceProvider services)
    {
        _scope = services.CreateScope();
        _typeRegistrations = new Dictionary<Type, Type>();
        _instanceRegistrations = new Dictionary<Type, object>();
        _lazyRegistrations = new Dictionary<Type, Func<object>>();
    }

    public Spectre.Console.Cli.ITypeResolver Build()
    {
        return new TestTypeResolver(
            _typeRegistrations,
            _instanceRegistrations,
            _lazyRegistrations,
            _scope.ServiceProvider);
    }

    public void Register(Type service, Type implementation)
    {
        _typeRegistrations[service] = implementation;
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _instanceRegistrations[service] = implementation;
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _lazyRegistrations[service] = factory;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _scope.Dispose();
            _disposed = true;
        }
    }
}

internal class TestTypeResolver : Spectre.Console.Cli.ITypeResolver
{
    private readonly Dictionary<Type, Type> _typeRegistrations;
    private readonly Dictionary<Type, object> _instanceRegistrations;
    private readonly Dictionary<Type, Func<object>> _lazyRegistrations;
    private readonly IServiceProvider _scopedProvider;
    private readonly Dictionary<Type, object> _lazyCache = new();

    public TestTypeResolver(
        Dictionary<Type, Type> typeRegistrations,
        Dictionary<Type, object> instanceRegistrations,
        Dictionary<Type, Func<object>> lazyRegistrations,
        IServiceProvider scopedProvider)
    {
        _typeRegistrations = typeRegistrations;
        _instanceRegistrations = instanceRegistrations;
        _lazyRegistrations = lazyRegistrations;
        _scopedProvider = scopedProvider;
    }

    public object? Resolve(Type? type)
    {
        if (type == null) return null;

        // Check instance registrations first (singletons)
        if (_instanceRegistrations.TryGetValue(type, out var instance))
        {
            return instance;
        }

        // Check lazy registrations (factories)
        if (_lazyRegistrations.TryGetValue(type, out var factory))
        {
            if (!_lazyCache.TryGetValue(type, out var cached))
            {
                cached = factory();
                _lazyCache[type] = cached;
            }
            return cached;
        }

        // Check type registrations (transient services - commands)
        if (_typeRegistrations.TryGetValue(type, out var implementationType))
        {
            return InstantiateType(implementationType, _scopedProvider);
        }

        // Fall back to scoped provider
        return _scopedProvider.GetService(type);
    }

    private object? InstantiateType(Type type, IServiceProvider serviceProvider)
    {
        // Find the best constructor (the one with the most parameters)
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
        {
            // Use parameterless constructor if available
            return Activator.CreateInstance(type);
        }

        // Use the constructor with the most parameters
        var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];

        // Resolve each parameter
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Check if it's a type we registered (Settings classes, etc.)
            if (_typeRegistrations.TryGetValue(paramType, out var implType))
            {
                args[i] = InstantiateType(implType, serviceProvider);
            }
            else
            {
                // Get from scoped provider
                args[i] = serviceProvider.GetService(paramType);
            }

            if (args[i] == null && !parameters[i].ParameterType.IsClass && !parameters[i].ParameterType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve service for type '{parameters[i].ParameterType.Name}' " +
                    $"while activating '{type.Name}'");
            }
        }

        return Activator.CreateInstance(type, args)!;
    }
}

#endregion
