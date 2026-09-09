#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Utilities;

namespace JobScheduler.Core.Formatters;

/// <summary>
/// Formatter for exporting job and execution data to CSV format.
/// Provides options for field selection and custom headers.
/// WHY: CSV export enables data analysis in Excel and other business intelligence tools.
/// </summary>
public sealed class CsvExportFormatter
{
    /// <summary>
    /// Exports jobs to CSV format.
    /// </summary>
    /// <param namejobs">The collection of jobs to export.</param>
    /// <returns>A CSV string representing the jobs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jobs"/> is null.</exception>
    public static string ExportJobsToCsv(IEnumerable<Job> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var sb = new StringBuilder();

        // Header row
        var headers = new[] { "ID", "Name", "Description", "CronExpression", "Priority", "Status",
                            "Active", "HandlerType", "MaxRetries", "ExecutionTimeout",
                            "NextExecution", "LastExecution", "TotalExecutions", "SuccessRate" };
        sb.AppendLine(string.Join(",", headers.Select(h => ParseUtility.EscapeCsvField(h))));

        // Data rows
        foreach (var job in jobs)
        {
            var row = new[]
            {
                ParseUtility.EscapeCsvField(job.Id.ToString()),
                ParseUtility.EscapeCsvField(job.Name),
                ParseUtility.EscapeCsvField(job.Description),
                ParseUtility.EscapeCsvField(job.CronExpression),
                ParseUtility.EscapeCsvField(job.Priority.ToString()),
                ParseUtility.EscapeCsvField(job.Status.ToString()),
                ParseUtility.EscapeCsvField(job.IsActive.ToString()),
                ParseUtility.EscapeCsvField(job.HandlerType),
                ParseUtility.EscapeCsvField(job.MaxRetries.ToString()),
                ParseUtility.EscapeCsvField(job.ExecutionTimeoutSeconds.ToString()),
                ParseUtility.EscapeCsvField(job.NextExecutionAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty),
                ParseUtility.EscapeCsvField(job.LastExecutedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty),
                ParseUtility.EscapeCsvField(job.TotalExecutions.ToString()),
                ParseUtility.EscapeCsvField(job.GetSuccessRate().ToString("F2", CultureInfo.InvariantCulture))
            };

            sb.AppendLine(string.Join(",", row));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports job executions to CSV format.
    /// </summary>
    /// <param name="executions">The collection of job executions to export.</param>
    /// <returns>A CSV string representing the job executions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executions"/> is null.</exception>
    public static string ExportExecutionsToCsv(IEnumerable<JobExecution> executions)
    {
        ArgumentNullException.ThrowIfNull(executions);
        var sb = new StringBuilder();

        // Header row
        var headers = new[] { "ID", "JobID", "Status", "StartedAt", "CompletedAt",
                            "ExecutionTime(ms)", "ErrorMessage", "RetryAttempt", "Output" };
        sb.AppendLine(string.Join(",", headers.Select(h => ParseUtility.EscapeCsvField(h))));

        // Data rows
        foreach (var execution in executions)
        {
            var row = new[]
            {
                ParseUtility.EscapeCsvField(execution.Id.ToString()),
                ParseUtility.EscapeCsvField(execution.JobId.ToString()),
                ParseUtility.EscapeCsvField(execution.Status.ToString()),
                ParseUtility.EscapeCsvField(execution.StartedAt.ToString("o", CultureInfo.InvariantCulture)),
                ParseUtility.EscapeCsvField(execution.CompletedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty),
                ParseUtility.EscapeCsvField(execution.ExecutionTimeMs.ToString()),
                ParseUtility.EscapeCsvField(execution.ErrorMessage ?? string.Empty),
                ParseUtility.EscapeCsvField(execution.RetryAttempt.ToString()),
                ParseUtility.EscapeCsvField(execution.ExecutionOutput ?? string.Empty)
            };

            sb.AppendLine(string.Join(",", row));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports execution statistics to CSV format.
    /// Useful for performance analysis and trend tracking.
    /// </summary>
    /// <param name="stats">A dictionary mapping job IDs to their execution statistics (total, successful, average time).</param>
    /// <returns>A CSV string representing the execution statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stats"/> is null.</exception>
    public static string ExportStatisticsToCsv(Dictionary<Guid, (int Total, int Successful, long AvgTime)> stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("JobID,TotalExecutions,SuccessfulExecutions,SuccessRate,AverageExecutionTime(ms)");

        // Data rows
        foreach (var kvp in stats)
        {
            var (total, successful, avgTime) = kvp.Value;
            var successRate = total == 0 ? 0 : (double)successful / total * 100;

            var row = new[]
            {
                ParseUtility.EscapeCsvField(kvp.Key.ToString()),
                ParseUtility.EscapeCsvField(total.ToString()),
                ParseUtility.EscapeCsvField(successful.ToString()),
                ParseUtility.EscapeCsvField(successRate.ToString("F2", CultureInfo.InvariantCulture)),
                ParseUtility.EscapeCsvField(avgTime.ToString())
            };

            sb.AppendLine(string.Join(",", row));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts CSV back to list of objects.
    /// Supports parsing exported job data.
    /// </summary>
    /// <param name="csv">The CSV string to parse.</param>
    /// <returns>A list of JobCsvRow objects parsed from the CSV.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="csv"/> is null or empty.</exception>
    public static List<JobCsvRow> ParseJobsCsv(string csv)
    {
        ArgumentException.ThrowIfNullOrEmpty(csv);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return new();

        var jobs = new List<JobCsvRow>();

        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseUtility.ParseCsvLine(lines[i]);
            if (fields.Count < 14)
                continue;

            var job = new JobCsvRow
            {
                Id = Guid.TryParse(fields[0], out var id) ? id : Guid.Empty,
                Name = fields[1],
                Description = fields[2],
                CronExpression = fields[3],
                Priority = fields[4],
                Status = fields[5],
                IsActive = fields[6] == "True",
                HandlerType = fields[7],
                MaxRetries = ParseUtility.ParseInt(fields[8]),
                ExecutionTimeoutSeconds = ParseUtility.ParseInt(fields[9]),
                NextExecution = fields[10],
                LastExecution = fields[11],
                TotalExecutions = ParseUtility.ParseInt(fields[12]),
                SuccessRate = ParseUtility.ParseDouble(fields[13])
            };

            jobs.Add(job);
        }

        return jobs;
    }
}

/// <summary>
/// Represents a row of job data for CSV export/import.
/// </summary>
public sealed class JobCsvRow
{
    /// <summary>
    /// Gets or sets the unique identifier of the job.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the job.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cron expression that defines the job's schedule.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority level of the job.
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the job.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the job is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the type of handler that processes the job.
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for the job.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the execution timeout in seconds for the job.
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution time of the job (as string).
    /// </summary>
    public string NextExecution { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last execution time of the job (as string).
    /// </summary>
    public string LastExecution { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of times the job has been executed.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the success rate of the job as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }
}