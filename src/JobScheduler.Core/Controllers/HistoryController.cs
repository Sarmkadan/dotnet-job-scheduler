#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Exposes read-only endpoints for querying job execution history and aggregated statistics.
/// Supports both per-job and system-wide history views with flexible filtering.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HistoryController : ControllerBase
{
    private readonly JobHistoryService _historyService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(JobHistoryService historyService, ILogger<HistoryController> logger)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns a filtered, paginated list of execution records for a specific job.
    /// Results are ordered newest-first.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ExecutionResponse>>> GetJobHistory(
        Guid jobId,
        [FromQuery] ExecutionStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new JobHistoryQuery
            {
                Status = status,
                From = from,
                To = to,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _historyService.GetJobHistoryAsync(jobId, query);
            return Ok(result);
        }
        catch (JobNotFoundException)
        {
            return NotFound(new { error = "Job not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve job history" });
        }
    }

    /// <summary>
    /// Returns aggregated execution statistics for a specific job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobExecutionSummary>> GetJobSummary(
        Guid jobId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var summary = await _historyService.GetJobSummaryAsync(jobId, from, to);
            return Ok(summary);
        }
        catch (JobNotFoundException)
        {
            return NotFound(new { error = "Job not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving summary for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retrieve job summary" });
        }
    }

    /// <summary>
    /// Returns a filtered, paginated list of execution records across all jobs.
    /// Useful for monitoring the overall health of the scheduler.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ExecutionResponse>>> GetSystemHistory(
        [FromQuery] ExecutionStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = new JobHistoryQuery
            {
                Status = status,
                From = from,
                To = to,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _historyService.GetSystemHistoryAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system execution history");
            return StatusCode(500, new { error = "Failed to retrieve system history" });
        }
    }

    /// <summary>
    /// Returns aggregated execution statistics across all jobs.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<JobExecutionSummary>> GetSystemSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var summary = await _historyService.GetSystemSummaryAsync(from, to);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system execution summary");
            return StatusCode(500, new { error = "Failed to retrieve system summary" });
        }
    }
}
