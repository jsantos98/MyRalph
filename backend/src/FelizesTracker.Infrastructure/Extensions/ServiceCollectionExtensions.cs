using System.Diagnostics.CodeAnalysis;
using FelizesTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FelizesTracker.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the application database context to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="enableRetryOnFailure">Whether to enable retry on failure (default: true)</param>
    /// <returns>Service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or connectionString is null</exception>
    /// <exception cref="ArgumentException">Thrown when connectionString is empty</exception>
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        string connectionString,
        bool enableRetryOnFailure = true)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        // Validate connection string format
        if (!connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Connection string must start with 'Data Source=' for SQLite",
                nameof(connectionString));
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });

            // Note: SQLite doesn't support EnableRetryOnFailure like SQL Server does.
            // Retry logic is handled at the connection pool level by SQLite.

            // Disable sensitive data logging in production
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (environment != "Development")
            {
                options.EnableSensitiveDataLogging(false);
            }

            options.EnableDetailedErrors();
        });

        return services;
    }

    /// <summary>
    /// Adds the application database context using configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="connectionStringName">Name of connection string in configuration (default: "DefaultConnection")</param>
    /// <returns>Service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configuration is null</exception>
    [SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names", Justification = "Reviewed")]
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            throw new ArgumentException("Connection string name cannot be null or empty", nameof(connectionStringName));
        }

        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' not found in configuration. " +
                $"Please add a connection string named '{connectionStringName}' to your appsettings.json.");
        }

        return services.AddAppDbContext(connectionString);
    }

    /// <summary>
    /// Initializes the database on application startup
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created
        await dbContext.EnsureDatabaseCreatedAsync(cancellationToken);

        // Run pending migrations if any exist
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
