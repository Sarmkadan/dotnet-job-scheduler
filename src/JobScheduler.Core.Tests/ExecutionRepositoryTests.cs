using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobScheduler.Core.Tests;

public class ExecutionRepositoryTests
{
    private static JobSchedulerContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JobSchedulerContext(options);

        // Seed a few executions for testing
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();

        var executions = new List<JobExecution>
        {
            new JobExecution
            {
                Id = Guid.NewGuid(),
                JobId = jobId1,
                Status = ExecutionStatus.Success,
                StartedAt = DateTime.UtcNow.AddHours(-3),
                CompletedAt = DateTime.UtcNow.AddHours(-2),
                DurationMilliseconds = 3600000,
                IsRetryable = false
            },
            new JobExecution
            {
                Id = Guid.NewGuid(),
                JobId = jobId1,
                Status = ExecutionStatus.Running,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                IsRetryable = false
            },
            new JobExecution
            {
                Id = Guid.NewGuid(),
                JobId = jobId2,
                Status = ExecutionStatus.Failed,
                StartedAt = DateTime.UtcNow.AddHours(-5),
                CompletedAt = DateTime.UtcNow.AddHours(-4),
                DurationMilliseconds = 600000,
                IsRetryable = true
            },
            new JobExecution
            {
                Id = Guid.NewGuid(),
                JobId = jobId2,
                Status = ExecutionStatus.Failed,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddMinutes(-50),
                DurationMilliseconds = 600000,
                IsRetryable = false
            }
        };

        context.JobExecutions.AddRange(executions);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task GetLatestExecutionAsync_ReturnsMostRecentExecution()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var jobId = context.JobExecutions.First().JobId;

        var latest = await repo.GetLatestExecutionAsync(jobId);

        Assert.NotNull(latest);
        var expected = context.JobExecutions
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .First();

        Assert.Equal(expected.Id, latest!.Id);
    }

    [Fact]
    public async Task GetExecutionsByJobAsync_ReturnsAllExecutionsOrderedDesc()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var jobId = context.JobExecutions.First().JobId;

        var result = await repo.GetExecutionsByJobAsync(jobId);

        var expected = context.JobExecutions
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => e.Id);

        Assert.Equal(expected, result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetExecutionsByStatusAsync_FiltersByStatus()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);

        var result = await repo.GetExecutionsByStatusAsync(ExecutionStatus.Failed);

        var expectedIds = context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Failed)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => e.Id);

        Assert.Equal(expectedIds, result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetExecutionsByJobAndStatusAsync_ReturnsCorrectSubset()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var jobId = context.JobExecutions.First(e => e.Status == ExecutionStatus.Failed).JobId;

        var result = await repo.GetExecutionsByJobAndStatusAsync(jobId, ExecutionStatus.Failed);

        var expected = context.JobExecutions
            .Where(e => e.JobId == jobId && e.Status == ExecutionStatus.Failed)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => e.Id);

        Assert.Equal(expected, result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetCurrentlyRunningCountAsync_ReturnsCorrectCount()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var jobId = context.JobExecutions.First(e => e.Status == ExecutionStatus.Running).JobId;

        var count = await repo.GetCurrentlyRunningCountAsync(jobId);

        var expected = context.JobExecutions.Count(e => e.JobId == jobId && e.Status == ExecutionStatus.Running);
        Assert.Equal(expected, count);
    }

    [Fact]
    public async Task GetFailedExecutionsRequiringRetryAsync_OnlyReturnsRetryableFailed()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);

        var result = await repo.GetFailedExecutionsRequiringRetryAsync();

        var expected = context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Failed && e.IsRetryable)
            .OrderBy(e => e.CompletedAt)
            .Select(e => e.Id);

        Assert.Equal(expected, result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetExecutionsByDateRangeAsync_ReturnsExecutionsWithinRange()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var start = DateTime.UtcNow.AddHours(-4);
        var end = DateTime.UtcNow.AddHours(-2);

        var result = await repo.GetExecutionsByDateRangeAsync(start, end);

        var expected = context.JobExecutions
            .Where(e => e.StartedAt >= start && e.StartedAt <= end)
            .OrderByDescending(e => e.StartedAt)
            .Select(e => e.Id);

        Assert.Equal(expected, result.Select(e => e.Id));
    }

    [Fact]
    public async Task GetLatestExecutionAsync_NonExistingJob_ReturnsNull()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var unknownJobId = Guid.NewGuid();

        var result = await repo.GetLatestExecutionAsync(unknownJobId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExecutionsByJobAsync_EmptyResult_ReturnsEmptyEnumerable()
    {
        using var context = CreateContext();
        var repo = new ExecutionRepository(context);
        var unknownJobId = Guid.NewGuid();

        var result = await repo.GetExecutionsByJobAsync(unknownJobId);

        Assert.Empty(result);
    }
}
