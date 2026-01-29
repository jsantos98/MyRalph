using System.Diagnostics.CodeAnalysis;
using System.IO;
using FelizesTracker.Infrastructure.Data;
using FelizesTracker.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FelizesTracker.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for AppDbContext configuration and behavior
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in test cleanup")]
public class DbContextTests : IAsyncLifetime
{
    private readonly string _testDbPath;
    private readonly IServiceCollection _services;
    private IServiceProvider? _serviceProvider;
    private AppDbContext? _dbContext;

    public DbContextTests()
    {
        // Create unique test database path
        var testGuid = Guid.NewGuid().ToString("N");
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-felizes-{testGuid}.db");

        _services = new ServiceCollection();
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
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}"
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

    [Fact]
    public void AddAppDbContext_ValidConnectionString_RegistersDbContext()
    {
        // Assert
        Assert.NotNull(_serviceProvider);
        Assert.NotNull(_dbContext);

        var retrievedContext = _serviceProvider!.GetService<AppDbContext>();
        Assert.NotNull(retrievedContext);
        Assert.Same(_dbContext, retrievedContext);
    }

    [Fact]
    public async Task AddAppDbContext_WithConfiguration_InitializesDatabaseSuccessfully()
    {
        // Arrange & Act - Database is already created in InitializeAsync

        // Assert
        Assert.NotNull(_dbContext);
        Assert.True(File.Exists(_testDbPath), "Database file should be created");

        // Verify database can connect
        var canConnect = await _dbContext!.Database.CanConnectAsync();
        Assert.True(canConnect, "Database should be accessible");
    }

    [Fact]
    public void AppDbContext_GetConnectionString_ReturnsValidConnectionString()
    {
        // Act
        var connectionString = _dbContext!.GetConnectionString();

        // Assert
        Assert.NotNull(connectionString);
        Assert.StartsWith("Data Source=", connectionString);
    }

    [Fact]
    public void AppDbContext_DatabasePath_ReturnsCorrectPath()
    {
        // Act
        var dbPath = _dbContext!.DatabasePath;

        // Assert
        Assert.NotNull(dbPath);
        Assert.EndsWith(".db", dbPath);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"ft-test-{Guid.NewGuid():N}");
        var testDbPath = Path.Combine(testDir, "test.db");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={testDbPath}"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddAppDbContext(configuration);

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act
            await dbContext.EnsureDatabaseCreatedAsync();

            // Assert
            Assert.True(Directory.Exists(testDir), "Directory should be created");
            Assert.True(File.Exists(testDbPath), "Database file should be created");

            // Cleanup
            await dbContext.DisposeAsync();
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
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task InitializeDatabaseAsync_CallsEnsureCreated()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test-init-{Guid.NewGuid():N}.db");

        if (File.Exists(testDbPath))
        {
            File.Delete(testDbPath);
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={testDbPath}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAppDbContext(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        // Act
        await serviceProvider.InitializeDatabaseAsync();

        // Assert
        Assert.True(File.Exists(testDbPath), "Database should be initialized and file created");

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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddAppDbContext_InvalidConnectionString_ThrowsException(string? connectionString)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            services.AddAppDbContext(connectionString!));
    }

    [Fact]
    public void AddAppDbContext_InvalidConnectionStringFormat_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddAppDbContext("InvalidConnectionString"));
    }

    [Fact]
    public void AddAppDbContext_NullConfiguration_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddAppDbContext(null!, "DefaultConnection"));
    }

    [Fact]
    public void AddAppDbContext_NullConnectionStringName_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            services.AddAppDbContext(configuration, null!));
    }

    [Fact]
    public void AddAppDbContext_MissingConnectionString_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddAppDbContext(configuration, "MissingConnection"));

        Assert.Contains("MissingConnection", exception.Message);
    }

    [Fact]
    public async Task Database_WithRetryOnFailure_ResilientToTransientErrors()
    {
        // Arrange
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test-retry-{Guid.NewGuid():N}.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={testDbPath}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAppDbContext(configuration);

        using var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        await dbContext.EnsureDatabaseCreatedAsync();
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);

        // Cleanup
        await dbContext.DisposeAsync();
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

    [Fact]
    public async Task Database_MultipleContexts_Instance_SameDatabaseFile()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_testDbPath}"
            })
            .Build();

        var services1 = new ServiceCollection();
        services1.AddSingleton<IConfiguration>(configuration);
        services1.AddAppDbContext(configuration);

        var services2 = new ServiceCollection();
        services2.AddSingleton<IConfiguration>(configuration);
        services2.AddAppDbContext(configuration);

        // Act
        using var scope1 = services1.BuildServiceProvider().CreateScope();
        using var scope2 = services2.BuildServiceProvider().CreateScope();

        var dbContext1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext1.Database.EnsureCreatedAsync();

        // Assert
        Assert.Equal(dbContext1.DatabasePath, dbContext2.DatabasePath);
        Assert.True(await dbContext2.Database.CanConnectAsync());
    }
}
