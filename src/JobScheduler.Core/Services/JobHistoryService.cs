#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides rich querying, filtering, and aggregation over job execution history.
/// Complements the per-job history exposed by <see cref="JobSchedulerService"/> with
/// system-wide views, time-range filtering, and aggregated statistics.
/// </summary>
public sealed class JobHistoryService
{
    private readonly IExecutionRepository _executionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobHistoryService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobHistoryService"/>.
    /// </summary>
    public JobHistoryService(
        IExecutionRepository executionRepository,
        IJobRepository jobRepository,
        ILogger<JobHistoryService>? logger = null)
    {
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _logger = logger;
    }

    /// <summary>
    /// Returns a filtered, paginated list of execution records for a specific job.
    /// Executions are returned newest-first.
    /// </summary>
    /// <param name="jobId">The job to query history for.</param>
    /// <param name="query">Optional filters and pagination settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="JobNotFoundException">Thrown when the job does not exist.</exception>
    public async Task<PagedResult<ExecutionResponse>> GetJobHistoryAsync(
        Guid jobId,
        JobHistoryQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var jobExists = await _jobRepository.GetByIdAsync(jobId);
        if (jobExists is null)
            throw new JobNotFoundException(jobId);

        var normalized = (query ?? new JobHistoryQuery()).Normalize();

        IEnumerable<JobExecution> executions;

        if (normalized.From.HasValue || normalized.To.HasValue)
        {
            var from = normalized.From ?? DateTime.MinValue;
            var to = normalized.To ?? DateTime.MaxValue;
            executions = await _executionRepository.GetExecutionsByDateRangeAsync(from, to);
            executions = executions.Where(e => e.JobId == jobId);
        }
        else
        {
            executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        }

        if (normalized.Status.HasValue)
            executions = executions.Where(e => e.Status == normalized.Status.Value);

        var ordered = executions.OrderByDescending(e => e.StartedAt).ToList();
        var total = ordered.Count;
        var page = ordered
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(ExecutionResponse.FromExecution)
            .ToList();

        _logger?.LogDebug("Job {JobId} history query returned {Count}/{Total} records", jobId, page.Count, total);

        return new PagedResult<ExecutionResponse>(page, total, normalized.PageNumber, normalized.PageSize);
    }

    /// <summary>
    /// Returns a filtered, paginated list of execution records across all jobs.
    /// Executions are returned newest-first.
    /// </summary>
    /// <param name="query">Optional filters and pagination settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PagedResult<ExecutionResponse>> GetSystemHistoryAsync(
        JobHistoryQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = (query ?? new JobHistoryQuery()).Normalize();

        IEnumerable<JobExecution> executions;

        if (normalized.From.HasValue || normalized.To.HasValue)
        {
            var from = normalized.From ?? DateTime.MinValue;
            var to = normalized.To ?? DateTime.MaxValue;
            executions = await _executionRepository.GetExecutionsByDateRangeAsync(from, to);
        }
        else if (normalized.Status.HasValue)
        {
            executions = await _executionRepository.GetExecutionsByStatusAsync(normalized.Status.Value);
        }
        else
        {
            executions = await _executionRepository.GetRunningExecutionsAsync();
            var allStatuses = Enum.GetValues<ExecutionStatus>();
            var allExecs = new List<JobExecution>();
            foreach (var status in allStatuses)
                allExecs.AddRange(await _executionRepository.GetExecutionsByStatusAsync(status));
            executions = allExecs;
        }

        if (normalized.Status.HasValue)
            executions = executions.Where(e => e.Status == normalized.Status.Value);

        var ordered = executions
            .DistinctBy(e => e.Id)
            .OrderByDescending(e => e.StartedAt)
            .ToList();

        var total = ordered.Count;
        var page = ordered
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(ExecutionResponse.FromExecution)
            .ToList();

        _logger?.LogDebug("System history query returned {Count}/{Total} records", page.Count, total);

        return new PagedResult<ExecutionResponse>(page, total, normalized.PageNumber, normalized.PageSize);
    }

    /// <summary>
    /// Returns aggregated execution statistics for a specific job.
    /// Covers the full lifetime of the job unless <paramref name="from"/> or <paramref name="to"/> are set.
    /// </summary>
    /// <param name="jobId">The job to summarize.</param>
    /// <param name="from">Optional start of the analysis window (UTC).</param>
    /// <param name="to">Optional end of the analysis window (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="JobNotFoundException">Thrown when the job does not exist.</exception>
    public async Task<JobExecutionSummary> GetJobSummaryAsync(
        Guid jobId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        IEnumerable<JobExecution> executions;
        if (from.HasValue || to.HasValue)
        {
            executions = await _executionRepository.GetExecutionsByDateRangeAsync(
                from ?? DateTime.MinValue, to ?? DateTime.MaxValue);
            executions = executions.Where(e => e.JobId == jobId);
        }
        else
        {
            executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        }

        return BuildSummary(executions, jobId, job.Name);
    }

    /// <summary>
    /// Returns aggregated execution statistics across all jobs for the given time window.
    /// </summary>
    /// <param name="from">Optional start of the analysis window (UTC).</param>
    /// <param name="to">Optional end of the analysis window (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<JobExecutionSummary> GetSystemSummaryAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<JobExecution> executions;
        if (from.HasValue || to.HasValue)
        {
            executions = await _executionRepository.GetExecutionsByDateRangeAsync(
                from ?? DateTime.MinValue, to ?? DateTime.MaxValue);
        }
        else
        {
            var allStatuses = Enum.GetValues<ExecutionStatus>();
            var allExecs = new List<JobExecution>();
            foreach (var status in allStatuses)
                allExecs.AddRange(await _executionRepository.GetExecutionsByStatusAsync(status));
            executions = allExecs.DistinctBy(e => e.Id);
        }

        return BuildSummary(executions, jobId: null, jobName: null);
    }

    private static JobExecutionSummary BuildSummary(
        IEnumerable<JobExecution> executions,
        Guid? jobId,
        string? jobName)
    {
        var list = executions.ToList();
        if (list.Count == 0)
        {
            return new JobExecutionSummary
            {
                JobId = jobId,
                JobName = jobName,
                TotalExecutions = 0
            };
        }

        var durations = list
            .Where(e => e.DurationMilliseconds > 0)
            .Select(e => e.DurationMilliseconds)
            .ToList();

        var latest = list.OrderByDescending(e => e.StartedAt).First();

        return new JobExecutionSummary
        {
            JobId = jobId,
            JobName = jobName,
            TotalExecutions = list.Count,
            SuccessCount = list.Count(e => e.Status == ExecutionStatus.Success),
            FailureCount = list.Count(e => e.Status == ExecutionStatus.Failed),
            TimedOutCount = list.Count(e => e.Status == ExecutionStatus.TimedOut),
            CancelledCount = list.Count(e => e.Status == ExecutionStatus.Cancelled),
            AverageDurationMs = durations.Count > 0 ? (long)durations.Average() : 0,
            MinDurationMs = durations.Count > 0 ? durations.Min() : 0,
            MaxDurationMs = durations.Count > 0 ? durations.Max() : 0,
            LastExecutedAt = latest.StartedAt,
            LastStatus = latest.Status
        };
    }
}

/// <summary>
/// A paginated result set wrapping a collection of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total number of matching records across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Current page number (1-based).</summary>
    public int PageNumber { get; }

    /// <summary>Maximum items per page.</summary>
    public int PageSize { get; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
