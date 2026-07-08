#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Services;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Manages job CRUD operations, lifecycle management, and status updates.
/// Provides comprehensive job scheduling and configuration endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly JobSchedulerService _schedulerService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(JobSchedulerService schedulerService, ILogger<JobsController> logger)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new scheduled job with validation and cron expression checking.
    /// Returns 201 Created with the newly created job details.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobResponse>> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            var job = new Job
            {
                Name = request.Name,
                Description = request.Description,
                CronExpression = request.CronExpression,
                HandlerType = request.HandlerType,
                HandlerParameters = request.HandlerParameters,
                Priority = request.Priority,
                MaxRetries = request.MaxRetries,
                RetryBackoffSeconds = request.RetryBackoffSeconds,
                ExecutionTimeoutSeconds = request.ExecutionTimeoutSeconds,
                MaxConcurrentExecutions = request.MaxConcurrentExecutions
            };

            var createdJob = await _schedulerService.CreateJobAsync(job, User?.Identity?.Name);

            _logger.LogInformation("Job created: {JobId} - {JobName}", createdJob.Id, createdJob.Name);

            return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, MapToResponse(createdJob));
        }
        catch (JobValidationException ex)
        {
            _logger.LogWarning("Job validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (CronExpressionException ex)
        {
            _logger.LogWarning("Invalid cron expression: {Message}", ex.Message);
            return BadRequest(new { error = "Invalid cron expression: " + ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new { error = "Failed to create job" });
        }
    }

    /// <summary>
    /// Retrieves a specific job by ID with full details and execution history.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobResponse>> GetJob(Guid id)
    {
        try
        {
            var job = await _schedulerService.GetJobByIdAsync(id);
            if (job is null)
            {
                _logger.LogWarning("Job not found: {JobId}", id);
                return NotFound(new { error = "Job not found" });
            }

            return Ok(MapToResponse(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to retrieve job" });
        }
    }

    /// <summary>
    /// Lists all jobs with optional filtering by status, priority, and active state.
    /// Supports pagination for large result sets.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<JobResponse>>> ListJobs(
        [FromQuery] JobStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var jobs = await _schedulerService.GetJobsAsync(status, pageNumber, pageSize);
            var total = await _schedulerService.GetTotalJobCountAsync(status);

            var responses = jobs.Select(MapToResponse).ToList();

            return Ok(new PaginatedResponse<JobResponse>
            {
                Data = responses,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jobs");
            return StatusCode(500, new { error = "Failed to list jobs" });
        }
    }

    /// <summary>
    /// Updates an existing job's configuration, cron expression, and retry settings.
    /// Performs validation before persisting changes.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobResponse>> UpdateJob(Guid id, [FromBody] CreateJobRequest request)
    {
        try
        {
            var updated = await _schedulerService.UpdateJobAsync(id, request, User?.Identity?.Name);
            if (updated is null)
                return NotFound(new { error = "Job not found" });

            _logger.LogInformation("Job updated: {JobId}", id);
            return Ok(MapToResponse(updated));
        }
        catch (JobValidationException ex)
        {
            _logger.LogWarning("Job validation failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to update job" });
        }
    }

    /// <summary>
    /// Deletes a job and its entire execution history.
    /// This operation is irreversible.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        try
        {
            var success = await _schedulerService.DeleteJobAsync(id, User?.Identity?.Name);
            if (!success)
                return NotFound(new { error = "Job not found" });

            _logger.LogInformation("Job deleted: {JobId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to delete job" });
        }
    }

    /// <summary>
    /// Suspends a job, preventing it from executing until resumed.
    /// Used for maintenance or investigation purposes.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobResponse>> SuspendJob(Guid id, [FromBody] SuspendJobRequest? request = null)
    {
        try
        {
            var job = await _schedulerService.SuspendJobAsync(id, request?.Reason, User?.Identity?.Name);
            if (job is null)
                return NotFound(new { error = "Job not found" });

            _logger.LogInformation("Job suspended: {JobId}", id);
            return Ok(MapToResponse(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to suspend job" });
        }
    }

    /// <summary>
    /// Resumes a suspended job, allowing it to execute again.
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobResponse>> ResumeJob(Guid id)
    {
        try
        {
            var job = await _schedulerService.ResumeJobAsync(id, User?.Identity?.Name);
            if (job is null)
                return NotFound(new { error = "Job not found" });

            _logger.LogInformation("Job resumed: {JobId}", id);
            return Ok(MapToResponse(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to resume job" });
        }
    }

    /// <summary>
    /// Triggers immediate execution of a job, bypassing cron schedule.
    /// Useful for manual job execution and testing.
    /// </summary>
    [HttpPost("{id:guid}/execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExecutionResponse>> TriggerJobExecution(Guid id)
    {
        try
        {
            var execution = await _schedulerService.TriggerJobExecutionAsync(id);
            if (execution is null)
                return NotFound(new { error = "Job not found" });

            _logger.LogInformation("Job triggered for execution: {JobId}", id);
            return Ok(MapExecutionToResponse(execution));
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning("Concurrency limit exceeded: {Message}", ex.Message);
            return Conflict(new { error = "Job is already at max concurrent executions" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to execute job" });
        }
    }

    /// <summary>
    /// Returns past execution records for a job, newest first.
    /// Useful for monitoring and debugging job behaviour over time.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExecutionResponse>>> GetJobExecutionHistory(
        Guid id,
        [FromQuery] int limit = 20)
    {
        try
        {
            var executions = await _schedulerService.GetExecutionHistoryAsync(id, limit);
            return Ok(executions.Select(ExecutionResponse.FromExecution));
        }
        catch (JobNotFoundException)
        {
            return NotFound(new { error = "Job not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution history for job: {JobId}", id);
            return StatusCode(500, new { error = "Failed to retrieve execution history" });
        }
    }

    private JobResponse MapToResponse(Job job) =>
        new JobResponse
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            CronExpression = job.CronExpression,
            Priority = job.Priority.ToString(),
            Status = job.Status.ToString(),
            IsActive = job.IsActive,
            HandlerType = job.HandlerType,
            MaxRetries = job.MaxRetries,
            ExecutionTimeoutSeconds = job.ExecutionTimeoutSeconds,
            LastExecutedAt = job.LastExecutedAt,
            NextExecutionAt = job.NextExecutionAt,
            TotalExecutions = job.TotalExecutions,
            SuccessfulExecutions = job.SuccessfulExecutions,
            SuccessRate = job.GetSuccessRate(),
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };

    private ExecutionResponse MapExecutionToResponse(JobExecution execution) =>
        new ExecutionResponse
        {
            Id = execution.Id,
            JobId = execution.JobId,
            Status = execution.Status.ToString(),
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            ExecutionTimeMs = execution.ExecutionTimeMs,
            ErrorMessage = execution.ErrorMessage,
            RetryAttempt = execution.RetryAttempt
        };
}

public sealed class SuspendJobRequest
{
    public string? Reason { get; set; }
}

public sealed class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
