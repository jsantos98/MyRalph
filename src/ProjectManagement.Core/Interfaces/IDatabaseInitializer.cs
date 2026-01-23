namespace ProjectManagement.Core.Interfaces;

/// <summary>
/// Interface for initializing the database
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Initializes the database - creates it if it doesn't exist, then runs migrations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if database was created, false if it already existed</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
}
