#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for managing and querying job schedules.
/// Provides schedule analysis, next run time calculation, and schedule history.
/// WHY: Separating schedule logic improves code organization and testability.
/// </summary>
public sealed class ScheduleService
{
    private readonly IJobRepository _jobRepository;
    private readonly CronExpressionService _cronService;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IJobRepository jobRepository,
        CronExpressionService cronService,
        ILogger<ScheduleService> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _cronService = cronService ?? throw new ArgumentNullException(nameof(cronService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the next N scheduled execution times for a job.
    /// Useful for displaying upcoming execution schedule to users.
    /// </summary>
    public async Task<List<DateTime>> GetUpcomingExecutionTimesAsync(Guid jobId, int count = 10)
    {
        try
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job is null || !job.IsActive)
                return new();

            var upcomingTimes = new List<DateTime>();
            var current = job.NextExecutionAt ?? DateTime.UtcNow;

            for (int i = 0; i < count; i++)
            {
                var next = _cronService.GetNextExecutionTime(job.CronExpression, current);
                upcomingTimes.Add(next);
                current = next.AddSeconds(1);
            }

            return upcomingTimes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming execution times for job: {JobId}", jobId);
            return new();
        }
    }

    /// <summary>
    /// Calculates the frequency of a cron expression in executions per day.
    /// </summary>
    public async Task<double> GetExecutionFrequencyPerDayAsync(string cronExpression)
    {
        try
        {
            var times = new List<DateTime>();
            var start = DateTime.UtcNow.Date;
            var end = start.AddDays(1);
            var current = start;

            while (current < end)
            {
                var next = _cronService.GetNextExecutionTime(cronExpression, current);
                if (next >= end)
                    break;

                times.Add(next);
                current = next.AddSeconds(1);
            }

            return times.Count;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets human-readable description of a cron expression.
    /// Example: "0 9 * * 1-5" -> "At 9:00 AM, Monday through Friday"
    /// </summary>
    public async Task<string> GetCronExpressionDescriptionAsync(string cronExpression)
    {
        try
        {
            // This would use a library like CronExpressionDescriptor
            // For now, return a basic description
            return _cronService.GetCronDescription(cronExpression) ?? "Custom schedule";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error describing cron expression: {Expression}", cronExpression);
            return "Invalid schedule";
        }
    }

    /// <summary>
    /// Estimates the number of executions in a given time period.
    /// Used for capacity planning and SLA calculations.
    /// </summary>
    public async Task<int> EstimateExecutionCountAsync(Guid jobId, int days)
    {
        try
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job is null)
                return 0;

            var frequency = await GetExecutionFrequencyPerDayAsync(job.CronExpression);
            return (int)(frequency * days);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating execution count for job: {JobId}", jobId);
            return 0;
        }
    }

    /// <summary>
    /// Finds the next N jobs scheduled to execute.
    /// Used to determine scheduler's immediate workload.
    /// </summary>
    public async Task<List<Job>> GetNextScheduledJobsAsync(int count = 10)
    {
        try
        {
            var allJobs = await _jobRepository.GetAllAsync();

            var nextJobs = allJobs
                .Where(j => j.IsActive && j.NextExecutionAt.HasValue)
                .OrderBy(j => j.NextExecutionAt)
                .Take(count)
                .ToList();

            return nextJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next scheduled jobs");
            return new();
        }
    }

    /// <summary>
    /// Analyzes schedule distribution across hours of the day.
    /// Helps identify peak scheduling times and load balancing opportunities.
    /// </summary>
    public async Task<Dictionary<int, int>> GetScheduleDistributionByHourAsync()
    {
        try
        {
            var distribution = new Dictionary<int, int>();
            for (int i = 0; i < 24; i++)
                distribution[i] = 0;

            var allJobs = await _jobRepository.GetAllAsync();

            foreach (var job in allJobs.Where(j => j.IsActive))
            {
                var times = await GetUpcomingExecutionTimesAsync(job.Id, 30);
                foreach (var time in times)
                {
                    distribution[time.Hour]++;
                }
            }

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing schedule distribution");
            return new();
        }
    }

    /// <summary>
    /// Returns jobs that would execute in the next N minutes.
    /// </summary>
    public async Task<List<Job>> GetJobsExecutingInNextMinutesAsync(int minutes = 5)
    {
        try
        {
            var threshold = DateTime.UtcNow.AddMinutes(minutes);
            var allJobs = await _jobRepository.GetAllAsync();

            var jobs = allJobs
                .Where(j => j.IsActive &&
                           j.NextExecutionAt.HasValue &&
                           j.NextExecutionAt <= threshold)
                .OrderBy(j => j.NextExecutionAt)
                .ToList();

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs executing in next minutes");
            return new();
        }
    }
}
