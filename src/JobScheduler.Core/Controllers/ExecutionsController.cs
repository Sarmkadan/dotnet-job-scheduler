// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides access to job execution history, logs, and detailed execution metrics.
/// Enables tracking of individual execution attempts and failure reasons.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExecutionsController : ControllerBase
{
    private readonly JobSchedulerService _schedulerService;
    private readonly ExecutionStatisticsService _statisticsService;
    private readonly ILogger<ExecutionsController> _logger;

    public ExecutionsController(
        JobSchedulerService schedulerService,
        ExecutionStatisticsService statisticsService,
        ILogger<ExecutionsController> logger)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves paginated execution history for a specific job.
    /// Includes execution status, duration, and error details.
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<ExecutionResponse>>> GetJobExecutions(
        Guid jobId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var executions = await _schedulerService.GetJobExecutionsAsync(jobId, pageNumber, pageSize);
            if (executions == null)
            {
                _logger.LogWarning("Job not found for executions: {JobId}", jobId);
                return NotFound(new { error = "Job not found" });
            }

            var total = await _schedulerService.GetJobExecutionCountAsync(jobId);
            var responses = executions.Select(e => new ExecutionResponse
            {
                Id = e.Id,
                JobId = e.JobId,
                Status = e.Status.ToString(),
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                ExecutionTimeMs = e.ExecutionTimeMs,
                ErrorMessage = e.ErrorMessage,
                RetryAttempt = e.RetryAttempt
            }).ToList();

            return Ok(new PaginatedResponse<ExecutionResponse>
            {
                Data = responses,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving executions for job: {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve executions" });
        }
    }

    /// <summary>
    /// Retrieves a single execution by ID with complete details.
    /// Useful for detailed failure analysis and debugging.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionDetailsResponse>> GetExecution(Guid id)
    {
        try
        {
            var execution = await _schedulerService.GetExecutionByIdAsync(id);
            if (execution == null)
            {
                _logger.LogWarning("Execution not found: {ExecutionId}", id);
                return NotFound(new { error = "Execution not found" });
            }

            var response = new ExecutionDetailsResponse
            {
                Id = execution.Id,
                JobId = execution.JobId,
                JobName = execution.Job?.Name ?? string.Empty,
                Status = execution.Status.ToString(),
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                ExecutionTimeMs = execution.ExecutionTimeMs,
                ErrorMessage = execution.ErrorMessage,
                RetryAttempt = execution.RetryAttempt,
                MaxRetries = execution.Job?.MaxRetries ?? 0,
                Output = execution.ExecutionOutput
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution: {ExecutionId}", id);
            return StatusCode(500, new { error = "Failed to retrieve execution" });
        }
    }

    /// <summary>
    /// Gets execution statistics for a specific job including success rates and performance metrics.
    /// Returns aggregated data across all executions.
    /// </summary>
    [HttpGet("job/{jobId:guid}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionStatsResponse>> GetJobStatistics(Guid jobId)
    {
        try
        {
            var stats = await _statisticsService.GetJobExecutionStatsAsync(jobId);
            if (stats == null)
            {
                _logger.LogWarning("No statistics found for job: {JobId}", jobId);
                return NotFound(new { error = "Job not found" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job statistics: {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Retrieves recent failed executions across all jobs for quick failure tracking.
    /// Useful for monitoring and alerting purposes.
    /// </summary>
    [HttpGet("recent-failures")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExecutionResponse>>> GetRecentFailures(
        [FromQuery] int days = 7,
        [FromQuery] int limit = 50)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var failures = await _schedulerService.GetRecentFailedExecutionsAsync(cutoffDate, limit);

            var responses = failures.Select(e => new ExecutionResponse
            {
                Id = e.Id,
                JobId = e.JobId,
                Status = e.Status.ToString(),
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                ExecutionTimeMs = e.ExecutionTimeMs,
                ErrorMessage = e.ErrorMessage,
                RetryAttempt = e.RetryAttempt
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent failures");
            return StatusCode(500, new { error = "Failed to retrieve failures" });
        }
    }

    /// <summary>
    /// Retrieves execution performance analysis including slowest and fastest runs.
    /// Helps identify performance bottlenecks and optimization opportunities.
    /// </summary>
    [HttpGet("job/{jobId:guid}/performance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PerformanceAnalysisResponse>> GetJobPerformance(Guid jobId)
    {
        try
        {
            var analysis = await _statisticsService.GetJobPerformanceAnalysisAsync(jobId);
            if (analysis == null)
            {
                _logger.LogWarning("No performance data for job: {JobId}", jobId);
                return NotFound(new { error = "Job not found" });
            }

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance analysis: {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve performance data" });
        }
    }

    /// <summary>
    /// Clears old execution records based on retention policy.
    /// Helps maintain database performance by removing stale data.
    /// </summary>
    [HttpDelete("cleanup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CleanupResponse>> CleanupOldExecutions(
        [FromQuery] int olderThanDays = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = await _schedulerService.DeleteExecutionsOlderThanAsync(cutoffDate);

            _logger.LogInformation("Cleaned up {DeletedCount} old executions older than {CutoffDate}",
                deletedCount, cutoffDate);

            return Ok(new CleanupResponse
            {
                DeletedCount = deletedCount,
                CutoffDate = cutoffDate,
                Message = $"Successfully deleted {deletedCount} old executions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
            return StatusCode(500, new { error = "Failed to cleanup old executions" });
        }
    }
}

public class ExecutionDetailsResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryAttempt { get; set; }
    public int MaxRetries { get; set; }
    public string? Output { get; set; }
}

public class ExecutionStatsResponse
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MinExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public DateTime? LastExecutionAt { get; set; }
}

public class PerformanceAnalysisResponse
{
    public Guid JobId { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MedianExecutionTimeMs { get; set; }
    public long P95ExecutionTimeMs { get; set; }
    public long P99ExecutionTimeMs { get; set; }
    public long SlowestExecutionTimeMs { get; set; }
    public long FastestExecutionTimeMs { get; set; }
    public DateTime? SlowestExecutionAt { get; set; }
    public DateTime? FastestExecutionAt { get; set; }
}

public class CleanupResponse
{
    public int DeletedCount { get; set; }
    public DateTime CutoffDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
