// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides health check endpoints for monitoring and load balancer integration.
/// Enables external systems to verify scheduler availability and readiness.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly JobSchedulerService _schedulerService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(JobSchedulerService schedulerService, ILogger<HealthController> logger)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Quick liveness probe for load balancers. Returns 200 if service is running.
    /// This endpoint has minimal dependencies and should respond quickly.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe verifying the service can handle requests.
    /// Checks database connectivity and critical systems.
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var isConnected = await _schedulerService.IsDatabaseConnectedAsync();

            if (!isConnected)
            {
                _logger.LogWarning("Readiness check failed: database not connected");
                return StatusCode(503, new { status = "not_ready", reason = "Database connection failed" });
            }

            return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during readiness check");
            return StatusCode(503, new
            {
                status = "not_ready",
                reason = "Health check failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detailed health status check including all subsystems.
    /// Used by monitoring systems for comprehensive health assessment.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthStatusResponse>> GetStatus()
    {
        try
        {
            var response = new HealthStatusResponse
            {
                Timestamp = DateTime.UtcNow,
                Version = "1.1.0",
                Status = "OK"
            };

            // Check database
            try
            {
                response.Database.Available = await _schedulerService.IsDatabaseConnectedAsync();
                response.Database.LastChecked = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                response.Database.Available = false;
                response.Database.ErrorMessage = ex.Message;
                response.Status = "Degraded";
            }

            // Get system metrics
            try
            {
                var stats = await _schedulerService.GetSystemStatisticsAsync();
                response.Jobs.TotalCount = stats.TotalJobs;
                response.Jobs.ActiveCount = stats.ActiveJobs;
                response.Executions.TotalCount = stats.TotalExecutions;
                response.Executions.SuccessRate = stats.AverageSuccessRate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving system statistics");
                response.Status = "Degraded";
            }

            // Check memory
            response.Memory.UsageMb = GC.TotalMemory(false) / 1024 / 1024;
            response.Memory.Threshold = 2048; // 2GB

            if (response.Memory.UsageMb > response.Memory.Threshold)
                response.Status = "Degraded";

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health status");
            return StatusCode(500, new
            {
                status = "ERROR",
                error = "Failed to generate health status"
            });
        }
    }

    /// <summary>
    /// Detailed diagnostics endpoint for troubleshooting and support.
    /// Returns comprehensive system information and recent errors.
    /// </summary>
    [HttpGet("diagnostics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DiagnosticsResponse>> GetDiagnostics()
    {
        try
        {
            var diagnostics = new DiagnosticsResponse
            {
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                RuntimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            };

            // Memory information
            diagnostics.Memory = new MemoryDiagnostics
            {
                TotalMemoryMb = GC.TotalMemory(false) / 1024 / 1024,
                ManagedHeapSizeMb = GC.GetTotalMemory(false) / 1024 / 1024,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };

            // System statistics
            var stats = await _schedulerService.GetSystemStatisticsAsync();
            diagnostics.SystemStatistics = new SystemDiagnostics
            {
                TotalJobs = stats.TotalJobs,
                ActiveJobs = stats.ActiveJobs,
                TotalExecutions = stats.TotalExecutions,
                AverageSuccessRate = stats.AverageSuccessRate,
                AverageExecutionTimeMs = stats.AverageExecutionTimeMs
            };

            // Recent errors
            diagnostics.RecentErrors = await GetRecentErrorsSummary();

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving diagnostics");
            return StatusCode(500, new { error = "Failed to retrieve diagnostics" });
        }
    }

    private async Task<List<ErrorLogEntry>> GetRecentErrorsSummary()
    {
        try
        {
            var failures = await _schedulerService.GetRecentFailedExecutionsAsync(
                DateTime.UtcNow.AddHours(-24), 10);

            return failures
                .Where(f => !string.IsNullOrEmpty(f.ErrorMessage))
                .GroupBy(f => f.ErrorMessage)
                .Select(g => new ErrorLogEntry
                {
                    Message = g.Key ?? string.Empty,
                    Count = g.Count(),
                    LastOccurred = g.Max(f => f.StartedAt)
                })
                .ToList();
        }
        catch
        {
            return new List<ErrorLogEntry>();
        }
    }
}

public class HealthStatusResponse
{
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.1.0";
    public string Status { get; set; } = "OK";
    public DatabaseStatus Database { get; set; } = new();
    public JobsStatus Jobs { get; set; } = new();
    public ExecutionsStatus Executions { get; set; } = new();
    public MemoryStatus Memory { get; set; } = new();
}

public class DatabaseStatus
{
    public bool Available { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}

public class JobsStatus
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
}

public class ExecutionsStatus
{
    public int TotalCount { get; set; }
    public double SuccessRate { get; set; }
}

public class MemoryStatus
{
    public long UsageMb { get; set; }
    public long Threshold { get; set; }
}

public class DiagnosticsResponse
{
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string RuntimeVersion { get; set; } = string.Empty;
    public MemoryDiagnostics Memory { get; set; } = new();
    public SystemDiagnostics SystemStatistics { get; set; } = new();
    public List<ErrorLogEntry> RecentErrors { get; set; } = new();
}

public class MemoryDiagnostics
{
    public long TotalMemoryMb { get; set; }
    public long ManagedHeapSizeMb { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}

public class SystemDiagnostics
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int TotalExecutions { get; set; }
    public double AverageSuccessRate { get; set; }
    public long AverageExecutionTimeMs { get; set; }
}

public class ErrorLogEntry
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? LastOccurred { get; set; }
}
