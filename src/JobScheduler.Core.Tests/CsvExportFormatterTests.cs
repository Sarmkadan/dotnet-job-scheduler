using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Formatters;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CsvExportFormatterTests
{
    private static readonly string ExpectedJobsHeader =
        "ID,Name,Description,CronExpression,Priority,Status,Active,HandlerType,MaxRetries,ExecutionTimeout,NextExecution,LastExecution,TotalExecutions,SuccessRate";

    private static readonly string ExpectedExecutionsHeader =
        "ID,JobID,Status,StartedAt,CompletedAt,ExecutionTime(ms),ErrorMessage,RetryAttempt,Output";

    private static readonly string ExpectedStatisticsHeader =
        "JobID,TotalExecutions,SuccessfulExecutions,SuccessRate,AverageExecutionTime(ms)";

    [Fact]
    public void ExportJobsToCsv_HappyPath_ReturnsCorrectCsv()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test,Job\"Name",
            Description = "A description, with a comma",
            CronExpression = "* * * * *",
            Priority = JobPriority.Medium,
            Status = JobStatus.Scheduled,
            IsActive = true,
            HandlerType = "MyHandler",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 120,
            NextExecutionAt = DateTime.UtcNow,
            LastExecutedAt = DateTime.UtcNow.AddMinutes(-5),
            TotalExecutions = 10
        };

        // Act
        var csv = CsvExportFormatter.ExportJobsToCsv(new[] { job });

        // Assert – header
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(ExpectedJobsHeader, lines[0].TrimEnd());

        // Assert – round‑trip parsing yields the original values
        var parsed = CsvExportFormatter.ParseJobsCsv(csv);
        Assert.Single(parsed);
        var parsedJob = parsed[0];

        Assert.Equal(job.Id, parsedJob.Id);
        Assert.Equal(job.Name, parsedJob.Name);
        Assert.Equal(job.Description, parsedJob.Description);
        Assert.Equal(job.CronExpression, parsedJob.CronExpression);
        Assert.Equal(job.Priority.ToString(), parsedJob.Priority);
        Assert.Equal(job.Status.ToString(), parsedJob.Status);
        Assert.Equal(job.IsActive, parsedJob.IsActive);
        Assert.Equal(job.HandlerType, parsedJob.HandlerType);
        Assert.Equal(job.MaxRetries, parsedJob.MaxRetries);
        Assert.Equal(job.ExecutionTimeoutSeconds, parsedJob.ExecutionTimeoutSeconds);
        Assert.Equal(job.TotalExecutions, parsedJob.TotalExecutions);
        // SuccessRate is calculated by the domain entity; we compare with a tolerance
        Assert.InRange(parsedJob.SuccessRate, 0.0, 100.0);
    }

    [Fact]
    public void ExportJobsToCsv_EmptyCollection_ReturnsHeaderOnly()
    {
        // Act
        var csv = CsvExportFormatter.ExportJobsToCsv(Array.Empty<Job>());

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Equal(ExpectedJobsHeader, lines[0].TrimEnd());
    }

    [Fact]
    public void ExportExecutionsToCsv_HappyPath_ReturnsCorrectCsv()
    {
        // Arrange
        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            Status = JobExecutionStatus.Completed,
            StartedAt = DateTime.UtcNow.AddSeconds(-30),
            CompletedAt = DateTime.UtcNow,
            ExecutionTimeMs = 2500,
            ErrorMessage = null,
            RetryAttempt = 1,
            ExecutionOutput = "Result payload"
        };

        // Act
        var csv = CsvExportFormatter.ExportExecutionsToCsv(new[] { execution });

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(ExpectedExecutionsHeader, lines[0].TrimEnd());
        Assert.Equal(2, lines.Length); // header + one row

        var fields = ParseUtility.ParseCsvLine(lines[1]);
        Assert.Equal(execution.Id.ToString(), fields[0]);
        Assert.Equal(execution.JobId.ToString(), fields[1]);
        Assert.Equal(execution.Status.ToString(), fields[2]);
        Assert.Equal(execution.StartedAt.ToString("o", CultureInfo.InvariantCulture), fields[3]);
        Assert.Equal(execution.CompletedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty, fields[4]);
        Assert.Equal(execution.ExecutionTimeMs.ToString(), fields[5]);
        Assert.Equal(execution.ErrorMessage ?? string.Empty, fields[6]);
        Assert.Equal(execution.RetryAttempt.ToString(), fields[7]);
        Assert.Equal(execution.ExecutionOutput ?? string.Empty, fields[8]);
    }

    [Fact]
    public void ExportStatisticsToCsv_HappyPath_ReturnsCorrectCsv()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var stats = new Dictionary<Guid, (int Total, int Successful, long AvgTime)>
        {
            [jobId] = (total: 20, successful: 15, avgTime: 1234)
        };

        // Act
        var csv = CsvExportFormatter.ExportStatisticsToCsv(stats);

        // Assert
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(ExpectedStatisticsHeader, lines[0].TrimEnd());
        Assert.Equal(2, lines.Length); // header + one data line

        var fields = ParseUtility.ParseCsvLine(lines[1]);
        Assert.Equal(jobId.ToString(), fields[0]);
        Assert.Equal("20", fields[1]);
        Assert.Equal("15", fields[2]);
        Assert.Equal(((double)15 / 20 * 100).ToString("F2", CultureInfo.InvariantCulture), fields[3]);
        Assert.Equal("1234", fields[4]);
    }

    [Fact]
    public void ParseJobsCsv_OnlyHeader_ReturnsEmptyList()
    {
        // Arrange
        var csv = ExpectedJobsHeader + "\n";

        // Act
        var result = CsvExportFormatter.ParseJobsCsv(csv);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseJobsCsv_MalformedLine_IsSkipped()
    {
        // Arrange: header + a line with only 5 fields (should be ignored)
        var malformed = "a,b,c,d,e";
        var csv = $"{ExpectedJobsHeader}\n{malformed}\n";

        // Act
        var result = CsvExportFormatter.ParseJobsCsv(csv);

        // Assert
        Assert.Empty(result);
    }
}
