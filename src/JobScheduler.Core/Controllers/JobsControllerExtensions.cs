#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using JobScheduler.Core.Domain.Models;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides extension methods for <see cref="JobsController"/> to enhance job management capabilities.
/// Includes convenience methods for bulk operations, status queries, and execution control.
/// </summary>
public static class JobsControllerExtensions
{
    /// <summary>
    /// Bulk creates multiple jobs from a collection of requests.
    /// Returns a paginated response containing all created jobs with their IDs.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="requests">Collection of job creation requests.</param>
    /// <param name="cultureInfo">Culture info for consistent formatting (defaults to InvariantCulture).</param>
    /// <returns>Paginated response with all created jobs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller"/> or <paramref name="requests"/> is null.</exception>
    public static async Task<ActionResult<PaginatedResponse<JobResponse>>> BulkCreateJobs(
        this JobsController controller,
        IEnumerable<CreateJobRequest> requests,
        CultureInfo? cultureInfo = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(requests);

        cultureInfo ??= CultureInfo.InvariantCulture;

        var createdJobs = new List<JobResponse>();
        var errors = new List<string>();

        foreach (var request in requests)
        {
            try
            {
                var result = await controller.CreateJob(request);
                if (result.Result is OkObjectResult okResult && okResult.Value is JobResponse jobResponse)
                {
                    createdJobs.Add(jobResponse);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create job '{request.Name}': {ex.Message}");
            }
        }

        var paginatedResponse = new PaginatedResponse<JobResponse>
        {
            Data = createdJobs,
            TotalCount = createdJobs.Count,
            PageNumber = 1,
            PageSize = createdJobs.Count
        };

        if (errors.Count > 0)
        {
            controller.Response.Headers.Add("X-Bulk-Create-Errors", string.Join("||", errors));
        }

        return controller.Ok(paginatedResponse);
    }

    /// <summary>
    /// Checks if a job with the specified ID exists.
    /// Returns 200 OK with a boolean value if the job exists, 404 NotFound otherwise.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="id">The job ID to check.</param>
    /// <returns>Action result with boolean indicating existence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller"/> is null.</exception>
    public static async Task<ActionResult<bool>> JobExists(
        this JobsController controller,
        Guid id)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var result = await controller.GetJob(id);
        if (result.Result is OkObjectResult)
        {
            return controller.Ok(true);
        }
        else if (result.Result is NotFoundObjectResult)
        {
            return controller.NotFound(false);
        }

        return controller.StatusCode(500, false);
    }

    /// <summary>
    /// Gets the execution status summary for a job, including success rate and recent executions.
    /// Returns detailed execution statistics and metrics.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="id">The job ID to get status for.</param>
    /// <param name="limit">Maximum number of recent executions to include.</param>
    /// <returns>Action result with execution status summary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller"/> is null.</exception>
    public static async Task<ActionResult<JobExecutionStatusSummary>> GetJobExecutionStatus(
        this JobsController controller,
        Guid id,
        int limit = 10)
    {
        ArgumentNullException.ThrowIfNull(controller);

        // Get job details
        var jobResult = await controller.GetJob(id);
        if (jobResult.Result is not OkObjectResult okJobResult || okJobResult.Value is not JobResponse jobResponse)
        {
            return controller.NotFound(new { error = "Job not found" });
        }

        // Get execution history
        var historyResult = await controller.GetJobExecutionHistory(id, limit);
        if (historyResult.Result is not OkObjectResult okHistoryResult || okHistoryResult.Value is not IEnumerable<ExecutionResponse> executions)
        {
            return controller.StatusCode(500, new { error = "Failed to retrieve execution history" });
        }

        var executionList = executions.ToList();
        var totalExecutions = jobResponse.TotalExecutions;
        var successfulExecutions = jobResponse.SuccessfulExecutions;
        var failedExecutions = totalExecutions - successfulExecutions;
        var successRate = totalExecutions > 0
            ? Math.Round((double)successfulExecutions / totalExecutions * 100, 2)
            : 0;

        var summary = new JobExecutionStatusSummary
        {
            JobId = jobResponse.Id,
            JobName = jobResponse.Name,
            Status = jobResponse.Status,
            TotalExecutions = totalExecutions,
            SuccessfulExecutions = successfulExecutions,
            FailedExecutions = failedExecutions,
            SuccessRatePercentage = successRate,
            LastExecutionStatus = executionList.FirstOrDefault()?.Status,
            RecentExecutions = executionList,
            AverageExecutionTimeMs = executionList.Any()
                ? executionList.Average(e => e.ExecutionTimeMs > 0 ? e.ExecutionTimeMs : 0)
                : 0,
            LastExecutionTime = executionList.FirstOrDefault()?.CompletedAt
        };

        return controller.Ok(summary);
    }

    /// <summary>
    /// Bulk suspends multiple jobs with optional reason.
    /// Returns a collection of results indicating success/failure for each job.
    /// </summary>
    /// <param name="controller">The controller instance.</param>
    /// <param name="jobIds">Collection of job IDs to suspend.</param>
    /// <param name="reason">Optional reason for suspension.</param>
    /// <returns>Collection of suspension results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller"/> or <paramref name="jobIds"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="jobIds"/> is empty.</exception>
    public static async Task<ActionResult<IReadOnlyList<BulkOperationResult>>> BulkSuspendJobs(
        this JobsController controller,
        IEnumerable<Guid> jobIds,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(jobIds);

        if (!jobIds.Any())
        {
            throw new ArgumentException("Collection cannot be empty.", nameof(jobIds));
        }

        var results = new List<BulkOperationResult>();
        var idList = jobIds.ToList();

        if (idList.Count == 0)
        {
            return controller.Ok(results.AsReadOnly());
        }

        foreach (var jobId in idList)
        {
            var result = await controller.SuspendJob(jobId, new SuspendJobRequest { Reason = reason });
            var success = result.Result is OkObjectResult okResult2 && okResult2.StatusCode == 200;
            var errorMessage = result.Result switch
            {
                NotFoundObjectResult => "Job not found",
                StatusCodeResult statusResult when statusResult.StatusCode == 500 => "Internal server error",
                _ => null
            };

            results.Add(new BulkOperationResult
            {
                JobId = jobId,
                Success = success,
                ErrorMessage = errorMessage
            });
        }

        return controller.Ok(results.AsReadOnly());
    }
}

/// <summary>
/// Represents the result of a bulk operation on a single job.
/// </summary>
public sealed class BulkOperationResult
{
    /// <summary>Gets the job ID.</summary>
    public Guid JobId { get; init; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the error message if the operation failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents a comprehensive summary of job execution status and metrics.
/// </summary>
public sealed class JobExecutionStatusSummary
{
    /// <summary>Gets the job ID.</summary>
    public Guid JobId { get; init; }

    /// <summary>Gets the job name.</summary>
    public string? JobName { get; init; }

    /// <summary>Gets the job status.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the total number of executions.</summary>
    public int TotalExecutions { get; init; }

    /// <summary>Gets the number of successful executions.</summary>
    public int SuccessfulExecutions { get; init; }

    /// <summary>Gets the number of failed executions.</summary>
    public int FailedExecutions { get; init; }

    /// <summary>Gets the success rate percentage.</summary>
    public double SuccessRatePercentage { get; init; }

    /// <summary>Gets the status of the last execution.</summary>
    public string? LastExecutionStatus { get; init; }

    /// <summary>Gets the recent executions.</summary>
    public IReadOnlyList<ExecutionResponse> RecentExecutions { get; init; } = new List<ExecutionResponse>();

    /// <summary>Gets the average execution time in milliseconds.</summary>
    public double AverageExecutionTimeMs { get; init; }

    /// <summary>Gets the time of the last execution.</summary>
    public DateTimeOffset? LastExecutionTime { get; init; }
}