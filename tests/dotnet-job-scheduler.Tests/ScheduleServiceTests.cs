#nullable enable

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Unit tests for <see cref="ScheduleService"/> functionality.
/// Tests the service methods that handle job scheduling, execution time calculation,
/// and cron expression processing.
/// </summary>
public sealed class ScheduleServiceTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<CronExpressionService> _cronServiceMock = new();
    private readonly Mock<ILogger<ScheduleService>> _loggerMock = new();

    /// <summary>
/// Creates a new instance of <see cref="ScheduleService"/> for testing.
/// </summary>
/// <returns>A configured <see cref="ScheduleService"/> instance with mock dependencies.</returns>
private ScheduleService CreateService() => new(
        _jobRepoMock.Object,
        _cronServiceMock.Object,
        _loggerMock.Object);

    /// <summary>
/// Creates a test job entity with default values.
/// </summary>
/// <param name="id">Optional job ID. If null, generates a new GUID.</param>
/// <returns>A configured <see cref="Job"/> instance for testing purposes.</returns>
private static Job CreateJob(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "test-job",
        CronExpression = "0 * * * *",
        HandlerType = "App.Jobs.Test, App",
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = 1,
        Status = JobStatus.Scheduled,
        IsActive = true,
        NextExecutionAt = DateTime.UtcNow.AddHours(1)
    };

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetUpcomingExecutionTimesAsync"/> returns multiple upcoming execution times
	/// for a valid, active job.
	/// </summary>
	public async Task GetUpcomingExecutionTimesAsync_WithValidJob_ReturnsMultipleTimes()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);
        var now = DateTime.UtcNow;

        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);

        _cronServiceMock.Setup(c => c.GetNextExecutionTime(job.CronExpression, It.IsAny<DateTime?>()))
            .Returns<string, DateTime?>((cron, current) => current.GetValueOrDefault().AddHours(1));

        var service = CreateService();

        // Act
        var times = await service.GetUpcomingExecutionTimesAsync(jobId, count: 5);

        // Assert
        times.Should().HaveCount(5);
        for (int i = 0; i < times.Count - 1; i++)
        {
            times[i].Should().BeBefore(times[i + 1]);
        }
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetUpcomingExecutionTimesAsync"/> returns empty collection
	/// when the job exists but is marked as inactive.
	/// </summary>
	public async Task GetUpcomingExecutionTimesAsync_WithInactiveJob_ReturnsEmpty()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);
        job.IsActive = false;

        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);

        var service = CreateService();

        // Act
        var times = await service.GetUpcomingExecutionTimesAsync(jobId, count: 5);

        // Assert
        times.Should().BeEmpty();
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetUpcomingExecutionTimesAsync"/> returns empty collection
	/// when the job does not exist in the repository.
	/// </summary>
	public async Task GetUpcomingExecutionTimesAsync_WithNonexistentJob_ReturnsEmpty()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);

        var service = CreateService();

        // Act
        var times = await service.GetUpcomingExecutionTimesAsync(Guid.NewGuid());

        // Assert
        times.Should().BeEmpty();
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetUpcomingExecutionTimesAsync"/> correctly respects the cron expression
	/// and returns execution times that match the specified schedule.
	/// </summary>
	public async Task GetUpcomingExecutionTimesAsync_RespectsCronExpression()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);
        job.CronExpression = "0 9 * * *"; // 9 AM daily

        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);

        var execution1 = DateTime.UtcNow.Date.AddHours(9);
        var execution2 = execution1.AddDays(1);
        var execution3 = execution2.AddDays(1);

        var executions = new Queue<DateTime>([execution1, execution2, execution3]);
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(job.CronExpression, It.IsAny<DateTime?>()))
            .Returns(() => executions.Count > 0 ? executions.Dequeue() : DateTime.MaxValue);

        var service = CreateService();

        // Act
        var times = await service.GetUpcomingExecutionTimesAsync(jobId, count: 3);

        // Assert
        times.Should().HaveCount(3);
        times[0].Hour.Should().Be(9);
        times[1].Hour.Should().Be(9);
        times[2].Hour.Should().Be(9);
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetUpcomingExecutionTimesAsync"/> returns 10 execution times by default
	/// when no count parameter is specified.
	/// </summary>
	public async Task GetUpcomingExecutionTimesAsync_WithDefaultCount_Returns10Times()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);

        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(job.CronExpression, It.IsAny<DateTime?>()))
            .Returns<string, DateTime?>((cron, current) => current.GetValueOrDefault().AddHours(1));

        var service = CreateService();

        // Act
        var times = await service.GetUpcomingExecutionTimesAsync(jobId);

        // Assert
        times.Should().HaveCount(10);
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetExecutionFrequencyPerDayAsync"/> correctly calculates the number of times a cron expression
	/// will execute in a single day.
	/// </summary>
	public async Task GetExecutionFrequencyPerDayAsync_CalculatesCorrectly()
    {
        // Arrange
        var cronExpression = "0 * * * *"; // Hourly

        var executionTimes = new Queue<DateTime>();
        for (int i = 0; i < 24; i++)
        {
            executionTimes.Enqueue(DateTime.UtcNow.Date.AddHours(i));
        }

        _cronServiceMock.Setup(c => c.GetNextExecutionTime(cronExpression, It.IsAny<DateTime?>()))
            .Returns(() => executionTimes.Count > 0 ? executionTimes.Dequeue() : DateTime.MaxValue);

        var service = CreateService();

        // Act
        var frequency = await service.GetExecutionFrequencyPerDayAsync(cronExpression);

        // Assert
        frequency.Should().Be(24);
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetExecutionFrequencyPerDayAsync"/> returns 1 for a daily cron expression
	/// that executes once per day at midnight.
	/// </summary>
	public async Task GetExecutionFrequencyPerDayAsync_WithDailySchedule_ReturnsOne()
    {
        // Arrange
        var cronExpression = "0 0 * * *"; // Daily at midnight

        var executions = new Queue<DateTime>();
        executions.Enqueue(DateTime.UtcNow.Date); // Midnight of the current day

        _cronServiceMock.Setup(c => c.GetNextExecutionTime(cronExpression, It.IsAny<DateTime?>()))
            .Returns(() => executions.Count > 0 ? executions.Dequeue() : DateTime.MaxValue);

        var service = CreateService();

        // Act
        var frequency = await service.GetExecutionFrequencyPerDayAsync(cronExpression);

        // Assert
        frequency.Should().Be(1);
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetExecutionFrequencyPerDayAsync"/> correctly calculates the frequency for an expression
	/// that runs every 5 minutes (288 times per day).
	/// </summary>
	public async Task GetExecutionFrequencyPerDayAsync_WithEveryFiveMinutes_ReturnsCorrectCount()
    {
        // Arrange
        var cronExpression = "*/5 * * * *"; // Every 5 minutes

        _cronServiceMock.Setup(c => c.GetNextExecutionTime(cronExpression, It.IsAny<DateTime?>()))
            .Returns<string, DateTime?>((cron, current) =>
            {
                // Round up to the next five minute boundary, as the real expression would.
                var from = current.GetValueOrDefault();
                var slot = new DateTime(from.Year, from.Month, from.Day, from.Hour, from.Minute - (from.Minute % 5), 0, from.Kind);
                return slot <= from ? slot.AddMinutes(5) : slot;
            });

        var service = CreateService();

        // Act
        var frequency = await service.GetExecutionFrequencyPerDayAsync(cronExpression);

        // Assert
        frequency.Should().Be(288); // 24 * 60 / 5
    }

    [Fact]
    	/// <summary>
	/// Tests that <see cref="ScheduleService.GetExecutionFrequencyPerDayAsync"/> gracefully handles invalid cron expressions
	/// and returns 0 instead of throwing exceptions.
	/// </summary>
	public async Task GetExecutionFrequencyPerDayAsync_HandlesException_ReturnsZero()
    {
        // Arrange
        var cronExpression = "invalid cron";
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(cronExpression, It.IsAny<DateTime?>()))
            .Throws<InvalidOperationException>();

        var service = CreateService();

        // Act
        var frequency = await service.GetExecutionFrequencyPerDayAsync(cronExpression);

        // Assert
        frequency.Should().Be(0);
    }
}
