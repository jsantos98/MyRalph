using Microsoft.EntityFrameworkCore;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data;

/// <summary>
/// Initializes the database by creating it if needed and running migrations
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ProjectManagementDbContext _context;

    public DatabaseInitializer(ProjectManagementDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Initializes the database - creates it if it doesn't exist, then runs migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if database was created, false if it already existed</returns>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        bool? canConnect = null;
        bool hasPendingMigrations = false;

        try
        {
            // Check if the database exists by trying to connect
            canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            // Database is corrupt or doesn't exist in a valid state
            canConnect = false;
        }

        // Only check for pending migrations if we can connect
        if (canConnect == true)
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                hasPendingMigrations = pendingMigrations.Any();
            }
            catch
            {
                // If we can't check migrations, treat as if database needs to be created
                canConnect = false;
            }
        }

        if (canConnect != true)
        {
            // Database doesn't exist or is corrupt, delete it and recreate using migrations
            try
            {
                // Try to delete the database file (will throw if it doesn't exist)
                await _context.Database.EnsureDeletedAsync(cancellationToken);
            }
            catch
            {
                // Ignore errors if database doesn't exist or can't be deleted
            }

            // Create the database using migrations
            await _context.Database.MigrateAsync(cancellationToken);
            return true;
        }

        // Database exists, run any pending migrations
        if (hasPendingMigrations)
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }

        return false;
    }
}
