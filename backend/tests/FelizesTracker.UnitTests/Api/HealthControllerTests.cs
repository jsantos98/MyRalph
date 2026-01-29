using System.Diagnostics.CodeAnalysis;
using System.IO;
using FelizesTracker.Api.Controllers;
using FelizesTracker.Api.DTOs;
using FelizesTracker.Infrastructure.Data;
using FelizesTracker.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FelizesTracker.UnitTests.Api;

/// <summary>
/// Unit tests for HealthController
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in test cleanup")]
public class HealthControllerTests : IAsyncLifetime
{
    private readonly string _testDbPath;
    private readonly IServiceCollection _services;
    private IServiceProvider? _serviceProvider;
    private AppDbContext? _dbContext;
    private readonly Mock<ILogger<HealthController>> _mockLogger;

    public HealthControllerTests()
    {
        // Create unique test database path
        var testGuid = Guid.NewGuid().ToString("N");
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-health-{testGuid}.db");

        _services = new ServiceCollection();
        _mockLogger = new Mock<ILogger<HealthController>>();
    }

    /// <summary>
    /// Initializes the test database before each test
    /// </summary>
    public async Task InitializeAsync()
    {
        // Clean up any existing test database
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        // Configure in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}",
                ["Application:Version"] = "1.0.0"
            })
            .Build();

        // Add services
        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddAppDbContext(configuration);

        _serviceProvider = _services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

        await _dbContext.EnsureDatabaseCreatedAsync();
    }

    /// <summary>
    /// Cleans up the test database after each test
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }

        // Clean up test database file
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private HealthController CreateController()
    {
        var configuration = _serviceProvider!.GetRequiredService<IConfiguration>();
        return new HealthController(_dbContext!, _mockLogger.Object, configuration);
    }

    [Fact]
    public async Task GetHealth_WithHealthyDatabase_ReturnsOkResult()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.Equal("healthy", response.Status);
        Assert.Equal("1.0.0", response.Version);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.Single(response.Checks);
        Assert.Equal("healthy", response.Checks["database"]);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithHealthyDatabase_ReturnsCorrectResponseStructure()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);

        // Verify all required fields
        Assert.NotNull(response.Status);
        Assert.NotNull(response.Version);
        Assert.NotNull(response.Checks);
        Assert.NotEmpty(response.Checks);
    }

    [Fact]
    public async Task GetHealth_DatabaseIsAccessible_ChecksDatabaseHealth()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.True(response.Checks.ContainsKey("database"));
        Assert.Equal("healthy", response.Checks["database"]);
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseCannotConnect_ReturnsServiceUnavailable()
    {
        // Arrange
        // Create a separate test database file for this test
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test-unhealthy-{Guid.NewGuid():N}.db");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={testDbPath}",
                    ["Application:Version"] = "1.0.0"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddAppDbContext(configuration);
            services.AddLogging();

            using var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            // Don't create the database - this will cause connection failure
            var controller = new HealthController(dbContext, _mockLogger.Object, configuration);

            // Act
            var result = await controller.GetHealth();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            var response = Assert.IsType<HealthResponse>(statusCodeResult.Value);
            Assert.Equal("unhealthy", response.Status);
            Assert.Equal(503, statusCodeResult.StatusCode);
            Assert.True(response.Checks.ContainsKey("database"));
            Assert.StartsWith("unhealthy", response.Checks["database"]);
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(testDbPath))
                {
                    File.Delete(testDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_LogsInformation()
    {
        // Arrange
        var controller = CreateController();

        // Act
        await controller.GetHealth();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health check completed successfully")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetHealth_WhenUnhealthy_LogsWarning()
    {
        // Arrange
        // Create a separate test database file for this test
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test-unhealthy-log-{Guid.NewGuid():N}.db");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={testDbPath}",
                    ["Application:Version"] = "1.0.0"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddAppDbContext(configuration);
            services.AddLogging();

            using var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            // Don't create the database - this will cause connection failure
            var controller = new HealthController(dbContext, _mockLogger.Object, configuration);

            // Act
            await controller.GetHealth();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health check completed with errors")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(testDbPath))
                {
                    File.Delete(testDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GetHealth_ReturnsVersionFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}",
                ["Application:Version"] = "2.5.0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAppDbContext(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        await dbContext.EnsureDatabaseCreatedAsync();

        var controller = new HealthController(dbContext, _mockLogger.Object, configuration);

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.Equal("2.5.0", response.Version);
    }

    [Fact]
    public async Task GetHealth_WhenVersionNotConfigured_UsesDefaultVersion()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}"
                // No Application:Version configured
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAppDbContext(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        await dbContext.EnsureDatabaseCreatedAsync();

        var controller = new HealthController(dbContext, _mockLogger.Object, configuration);

        // Act
        var result = await controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.Equal("1.0.0", response.Version);
    }

    [Fact]
    public async Task GetHealth_Timestamp_IsUtc()
    {
        // Arrange
        var controller = CreateController();
        var beforeCall = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await controller.GetHealth();
        var afterCall = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.InRange(response.Timestamp, beforeCall, afterCall);
    }

    [Fact]
    public void HealthController_RouteAttribute_ConfiguredCorrectly()
    {
        // Arrange & Act & Assert
        var controllerType = typeof(HealthController);
        var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);

        var routeAttribute = Assert.Single(routeAttributes);
        Assert.Equal("api/v1/[controller]", ((RouteAttribute)routeAttribute).Template);
    }

    [Fact]
    public void HealthController_HasHttpGetAttribute()
    {
        // Arrange
        var controllerType = typeof(HealthController);
        var method = controllerType.GetMethod(nameof(HealthController.GetHealth))!;

        // Act
        var httpGetAttributes = method.GetCustomAttributes(typeof(HttpGetAttribute), false);

        // Assert
        Assert.Single(httpGetAttributes);
    }

    [Fact]
    public void HealthController_HasResponseCacheAttribute()
    {
        // Arrange
        var controllerType = typeof(HealthController);
        var method = controllerType.GetMethod(nameof(HealthController.GetHealth))!;

        // Act
        var cacheAttributes = method.GetCustomAttributes(typeof(ResponseCacheAttribute), false);

        // Assert
        var cacheAttribute = Assert.Single(cacheAttributes) as ResponseCacheAttribute;
        Assert.NotNull(cacheAttribute);
        Assert.Equal(60, cacheAttribute.Duration);
    }
}
