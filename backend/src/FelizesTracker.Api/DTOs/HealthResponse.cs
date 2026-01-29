namespace FelizesTracker.Api.DTOs;

/// <summary>
/// Response model for health check endpoint
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status (healthy/unhealthy/degraded)
    /// </summary>
    public string Status { get; set; } = "healthy";

    /// <summary>
    /// ISO 8601 timestamp of when the health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Application version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Individual health check results
    /// </summary>
    public Dictionary<string, string> Checks { get; set; } = new();
}
