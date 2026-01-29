using Microsoft.EntityFrameworkCore;

namespace FelizesTracker.Infrastructure.Data;

/// <summary>
/// Application database context for FelizesTracker using SQLite
/// </summary>
public class AppDbContext : DbContext
{
    private string? _dbPath;

    /// <summary>
    /// Initializes a new instance of the AppDbContext class
    /// </summary>
    /// <param name="options">Context options</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the database file path
    /// </summary>
    public string DatabasePath
    {
        get
        {
            if (_dbPath == null)
            {
                // Try to extract from connection string
                var connectionString = Database.GetConnectionString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        connectionString,
                        @"Data Source\s*=\s*(.+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        _dbPath = match.Groups[1].Value.Trim();
                    }
                }

                // Fallback to default
                _dbPath ??= Path.Combine(AppContext.BaseDirectory, "felizes-tracker.db");
            }

            return _dbPath;
        }
    }

    /// <summary>
    /// Configures the context options
    /// </summary>
    /// <param name="optionsBuilder">Options builder</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = $"Data Source={_dbPath}";
            optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            });

            // Enable detailed errors in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (environment == "Development")
            {
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
            }
        }
    }

    /// <summary>
    /// Configures the model and creates the database file if it doesn't exist
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure database directory exists
        var dbDirectory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Apply any additional configurations here
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Ensures the database is created
    /// </summary>
    /// <returns>True if database was created, false if it already existed</returns>
    public async Task<bool> EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
    {
        var dbPath = DatabasePath;
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        return await Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the database connection string
    /// </summary>
    public string GetConnectionString()
    {
        return $"Data Source={_dbPath}";
    }
}
