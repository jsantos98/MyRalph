using FelizesTracker.Api.DTOs;
using FelizesTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FelizesTracker.Api.Controllers;

/// <summary>
/// Health check controller for monitoring system status
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;
    private readonly string _appVersion;

    public HealthController(
        AppDbContext dbContext,
        ILogger<HealthController> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appVersion = configuration["Application:Version"] ?? "1.0.0";
    }

    /// <summary>
    /// Health check endpoint to verify system status
    /// </summary>
    /// <returns>Health status information</returns>
    /// <response code="200">System is healthy</response>
    /// <response code="503">System is unhealthy</response>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> GetHealth()
    {
        var response = new HealthResponse
        {
            Version = _appVersion
        };
        var isHealthy = true;

        // Check database connectivity
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                response.Checks["database"] = "healthy";
                _logger.LogDebug("Database health check passed");
            }
            else
            {
                response.Checks["database"] = "unhealthy";
                response.Status = "unhealthy";
                isHealthy = false;
                _logger.LogWarning("Database health check failed: Cannot connect");
            }
        }
        catch (Exception ex)
        {
            response.Checks["database"] = $"unhealthy: {ex.Message}";
            response.Status = "unhealthy";
            isHealthy = false;
            _logger.LogError(ex, "Database health check failed with exception");
        }

        // Set overall status
        response.Status = isHealthy ? "healthy" : "unhealthy";

        if (isHealthy)
        {
            _logger.LogInformation("Health check completed successfully");
            return Ok(response);
        }
        else
        {
            _logger.LogWarning("Health check completed with errors");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }
}
