#nullable enable

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Contains unit tests for the <see cref="JobSchedulerService"/> class, ensuring correct job scheduling, management, and execution logic.
/// </summary>
public sealed class JobSchedulerServiceTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly Mock<JobExecutorService> _executorServiceMock = new(
        Mock.Of<IJobRepository>(), Mock.Of<IExecutionRepository>(), new ConcurrencyManager(Mock.Of<IExecutionRepository>()));
    private readonly Mock<CronExpressionService> _cronServiceMock = new();
    private readonly Mock<RetryService> _retryServiceMock = new(
        Mock.Of<IJobRepository>(), Mock.Of<IExecutionRepository>());
    private readonly Mock<ConcurrencyManager> _concurrencyManagerMock = new(
        Mock.Of<IExecutionRepository>());

    private JobSchedulerService CreateService() => new(
        _jobRepoMock.Object,
        _executionRepoMock.Object,
        _executorServiceMock.Object,
        _cronServiceMock.Object,
        _retryServiceMock.Object,
        _concurrencyManagerMock.Object);

    private static Job CreateValidJob(string name = "test-job") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CronExpression = "0 9 * * *",
        HandlerType = "TestApp.Jobs.TestJob, TestApp",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = 1,
        Status = JobStatus.Scheduled
    };

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> persists a valid job and returns the created job instance.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithValidJob_PersistsAndReturnsJob()
    {
        // Arrange
        var job = CreateValidJob();
        _jobRepoMock.Setup(r => r.GetByNameAsync(job.Name)).ReturnsAsync((Job?)null);
        _cronServiceMock.Setup(c => c.IsValidCronExpression(job.CronExpression)).Returns(true);
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(job.CronExpression))
            .Returns(DateTime.UtcNow.AddHours(1));

        // Act
        var service = CreateService();
        var result = await service.CreateJobAsync(job, "user@example.com");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(job.Name);
        result.Status.Should().Be(JobStatus.Scheduled);
        result.CreatedBy.Should().Be("user@example.com");
        result.NextExecutionAt.Should().NotBeNull();
        _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Once);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> throws an <see cref="ArgumentNullException"/> when the job is null.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithNullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateJobAsync(null!));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> throws an <see cref="ArgumentException"/> when the job name is empty.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithEmptyJobName_ThrowsArgumentException()
    {
        // Arrange
        var job = CreateValidJob();
        job.Name = string.Empty;
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> throws a <see cref="CronExpressionException"/> when the cron expression is invalid.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithInvalidCronExpression_ThrowsCronExpressionException()
    {
        // Arrange
        var job = CreateValidJob();
        job.CronExpression = "invalid";
        _jobRepoMock.Setup(r => r.GetByNameAsync(job.Name)).ReturnsAsync((Job?)null);
        _cronServiceMock.Setup(c => c.ParseCronExpression("invalid"))
            .Throws(new CronExpressionException("invalid", "Failed to parse expression"));
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CronExpressionException>(() => service.CreateJobAsync(job));
        ex.Message.Should().Contain("'invalid'");
        ex.Message.Should().Contain("Failed to parse expression");
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> rejects an invalid cron expression before anything is written to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithInvalidCronExpression_DoesNotPersistJob()
    {
        // Arrange
        var job = CreateValidJob();
        job.CronExpression = "not a cron";
        _cronServiceMock.Setup(c => c.ParseCronExpression("not a cron"))
            .Throws(new CronExpressionException("not a cron", "Failed to parse expression"));
        var service = CreateService();

        // Act
        await Assert.ThrowsAsync<CronExpressionException>(() => service.CreateJobAsync(job));

        // Assert
        _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> rejects an empty job Id with a <see cref="JobValidationException"/> naming the Id property.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithEmptyId_ThrowsJobValidationExceptionNamingId()
    {
        // Arrange
        var job = CreateValidJob();
        job.Id = Guid.Empty;
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
        ex.PropertyName.Should().Be(nameof(Job.Id));
        _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> rejects a job whose Id already exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithDuplicateId_ThrowsJobValidationExceptionNamingId()
    {
        // Arrange
        var job = CreateValidJob();
        _jobRepoMock.Setup(r => r.GetByNameAsync(job.Name)).ReturnsAsync((Job?)null);
        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(CreateValidJob("other-job"));
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
        ex.PropertyName.Should().Be(nameof(Job.Id));
        ex.Message.Should().Contain(job.Id.ToString());
        _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> rejects a job with an invalid retry policy, naming the property and its value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithNegativeRetryMaxRetries_ThrowsJobValidationExceptionNamingPropertyAndValue()
    {
        // Arrange
        var job = CreateValidJob();
        job.RetryPolicy = new RetryPolicy { JobId = job.Id, MaxRetries = -4 };
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
        ex.PropertyName.Should().Be(nameof(RetryPolicy.MaxRetries));
        ex.Message.Should().Contain("-4");
        _jobRepoMock.Verify(r => r.AddAsync(It.IsAny<Job>()), Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ValidateAllJobsAsync"/> completes without throwing when every stored job is valid.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ValidateAllJobsAsync_WithAllValidJobs_DoesNotThrow()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { CreateValidJob("a"), CreateValidJob("b") });
        var service = CreateService();

        // Act
        var exception = await Record.ExceptionAsync(() => service.ValidateAllJobsAsync());

        // Assert
        exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ValidateAllJobsAsync"/> reports every invalid job in one exception,
    /// including jobs whose cron expression fails to parse, and does not stop at the first bad job.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ValidateAllJobsAsync_WithSeveralInvalidJobs_ReportsAllOfThem()
    {
        // Arrange
        var badName = CreateValidJob("bad-name");
        badName.Name = string.Empty;

        var badCron = CreateValidJob("bad-cron");
        badCron.CronExpression = "99 99 99 99 99";
        _cronServiceMock.Setup(c => c.ParseCronExpression("99 99 99 99 99"))
            .Throws(new CronExpressionException("99 99 99 99 99", "Failed to parse expression"));

        var badRetry = CreateValidJob("bad-retry");
        badRetry.RetryPolicy = new RetryPolicy { JobId = badRetry.Id, MaxRetries = -1 };

        var valid = CreateValidJob("good");

        _jobRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { badName, badCron, badRetry, valid });
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<JobValidationException>(() => service.ValidateAllJobsAsync());
        ex.Message.Should().StartWith("Found 3 invalid jobs:");
        ex.Message.Should().Contain($"Job {badName.Id} ");
        ex.Message.Should().Contain($"Job {badCron.Id} (bad-cron): Invalid cron expression '99 99 99 99 99'");
        ex.Message.Should().Contain($"Job {badRetry.Id} (bad-retry): MaxRetries must be >= 0. Current value: -1");
        ex.Message.Should().NotContain($"Job {valid.Id} ");
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> throws a <see cref="JobValidationException"/> when a job with the same name already exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithDuplicateJobName_ThrowsJobValidationException()
    {
        // Arrange
        var job = CreateValidJob();
        var existingJob = CreateValidJob(job.Name);
        _jobRepoMock.Setup(r => r.GetByNameAsync(job.Name)).ReturnsAsync(existingJob);
        _cronServiceMock.Setup(c => c.IsValidCronExpression(job.CronExpression)).Returns(true);
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
        ex.Message.Should().Contain(job.Name);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> correctly calculates the next execution time when a timezone is specified.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithTimeZone_CalculatesNextExecutionInTimeZone()
    {
        // Arrange
        var job = CreateValidJob();
        job.TimeZoneId = "Eastern Standard Time";
        _jobRepoMock.Setup(r => r.GetByNameAsync(job.Name)).ReturnsAsync((Job?)null);
        _cronServiceMock.Setup(c => c.IsValidCronExpression(job.CronExpression)).Returns(true);
        var expectedNextRun = DateTime.UtcNow.AddHours(2);
        _cronServiceMock.Setup(c => c.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId, It.IsAny<DateTime?>()))
            .Returns(expectedNextRun);

        // Act
        var service = CreateService();
        var result = await service.CreateJobAsync(job);

        // Assert
        result.NextExecutionAt.Should().Be(expectedNextRun);
        _cronServiceMock.Verify(c => c.GetNextExecutionTimeInZone(
            job.CronExpression, job.TimeZoneId, It.IsAny<DateTime?>()), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.CreateJobAsync(Job, string?)"/> throws a <see cref="JobValidationException"/> when the job configuration is invalid.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CreateJobAsync_WithInvalidJobConfiguration_ThrowsJobValidationException()
    {
        // Arrange
        var job = CreateValidJob();
        job.HandlerType = string.Empty; // Invalid configuration
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobValidationException>(() => service.CreateJobAsync(job));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.SuspendJobAsync(Guid)"/> successfully suspends an active job.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SuspendJobAsync_WithActiveJob_SuspendsProperly()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateValidJob();
        job.Id = jobId;
        job.Status = JobStatus.Scheduled;
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        var service = CreateService();

        // Act
        await service.SuspendJobAsync(jobId);

        // Assert
        job.Status.Should().Be(JobStatus.Suspended);
        _jobRepoMock.Verify(r => r.Update(job), Times.Once);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.SuspendJobAsync(Guid)"/> throws a <see cref="JobNotFoundException"/> when the job does not exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SuspendJobAsync_WithNonexistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(() => service.SuspendJobAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ResumeJobAsync(Guid)"/> successfully resumes a suspended job.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResumeJobAsync_WithSuspendedJob_ResumesScheduling()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateValidJob();
        job.Id = jobId;
        job.Status = JobStatus.Suspended;
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(job.CronExpression))
            .Returns(DateTime.UtcNow.AddHours(1));
        var service = CreateService();

        // Act
        await service.ResumeJobAsync(jobId);

        // Assert
        job.Status.Should().Be(JobStatus.Scheduled);
        job.NextExecutionAt.Should().NotBeNull();
        _jobRepoMock.Verify(r => r.Update(job), Times.Once);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.DeleteJobAsync(Guid)"/> removes the job from the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteJobAsync_RemovesJobFromRepository()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateValidJob();
        job.Id = jobId;
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        var service = CreateService();

        // Act
        await service.DeleteJobAsync(jobId);

        // Assert
        _jobRepoMock.Verify(r => r.Remove(job), Times.Once);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.DeleteJobAsync(Guid)"/> throws a <see cref="JobNotFoundException"/> when the job does not exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteJobAsync_WithNonexistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(() => service.DeleteJobAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.UpdateJobScheduleAsync(Guid, string)"/> successfully updates a job's schedule.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateJobScheduleAsync_WithValidCron_UpdatesSchedule()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateValidJob();
        job.Id = jobId;
        const string newCron = "0 12 * * *";
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _cronServiceMock.Setup(c => c.IsValidCronExpression(newCron)).Returns(true);
        _cronServiceMock.Setup(c => c.GetNextExecutionTime(newCron))
            .Returns(DateTime.UtcNow.AddHours(2));
        var service = CreateService();

        // Act
        await service.UpdateJobScheduleAsync(jobId, newCron);

        // Assert
        job.CronExpression.Should().Be(newCron);
        _jobRepoMock.Verify(r => r.Update(job), Times.Once);
        _jobRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ExecuteDueJobsAsync()"/> successfully executes due jobs.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteDueJobsAsync_ExecutesDueJobs()
    {
        // Arrange
        var dueJob = CreateValidJob();
        var dueJobs = new List<Job> { dueJob };
        _jobRepoMock.Setup(r => r.GetScheduledJobsForExecutionAsync())
            .ReturnsAsync(dueJobs);
        _concurrencyManagerMock.Setup(c => c.CanExecuteAsync(dueJob))
            .ReturnsAsync(true);

        var mockExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = dueJob.Id,
            Status = ExecutionStatus.Success,
            CompletedAt = DateTime.UtcNow
        };
        _executorServiceMock.Setup(e => e.ExecuteJobAsync(dueJob, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExecution);

        var service = CreateService();

        // Act
        var executions = await service.ExecuteDueJobsAsync();

        // Assert
        executions.Should().HaveCount(1);
        executions.First().Status.Should().Be(ExecutionStatus.Success);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ExecuteDueJobsAsync()"/> skips jobs that exceed concurrency limits.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteDueJobsAsync_SkipsJobsExceedingConcurrency()
    {
        // Arrange
        var dueJob = CreateValidJob();
        dueJob.MaxConcurrentExecutions = 1;
        var dueJobs = new List<Job> { dueJob };
        _jobRepoMock.Setup(r => r.GetScheduledJobsForExecutionAsync())
            .ReturnsAsync(dueJobs);
        _concurrencyManagerMock.Setup(c => c.CanExecuteAsync(dueJob))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var executions = await service.ExecuteDueJobsAsync();

        // Assert
        executions.Should().BeEmpty();
        _executorServiceMock.Verify(e => e.ExecuteJobAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.GetJobDetailsAsync(Guid)"/> returns a job with its execution history.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetJobDetailsAsync_ReturnsJobWithExecutionHistory()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateValidJob();
        job.Id = jobId;
        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Status = ExecutionStatus.Success,
            CompletedAt = DateTime.UtcNow
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(jobId))
            .ReturnsAsync(new List<JobExecution> { execution });

        var service = CreateService();

        // Act
        var result = await service.GetJobDetailsAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result!.Job.Id.Should().Be(jobId);
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.GetJobDetailsAsync(Guid)"/> returns null when the job does not exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetJobDetailsAsync_WithNonexistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(() => service.GetJobDetailsAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that <see cref="JobSchedulerService.ProcessRetriesAsync()"/> triggers retries for failed executions when appropriate.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProcessRetriesAsync_RetriesFailedExecutions()
    {
        // Arrange
        var failedExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            Status = ExecutionStatus.Failed,
            AttemptNumber = 1,
            IsRetryable = true,
            CompletedAt = DateTime.UtcNow.AddSeconds(-10)
        };

        var job = CreateValidJob();
        job.Id = failedExecution.JobId;

        _executionRepoMock.Setup(r => r.GetFailedExecutionsRequiringRetryAsync())
            .ReturnsAsync(new List<JobExecution> { failedExecution });
        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _retryServiceMock.Setup(r => r.ShouldRetryAsync(job, failedExecution))
            .ReturnsAsync(true);

        var retryExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            AttemptNumber = 2,
            Status = ExecutionStatus.Running
        };
        _retryServiceMock.Setup(r => r.CreateRetryExecution(job, failedExecution))
            .Returns(retryExecution);

        var service = CreateService();

        // Act
        await service.ProcessRetriesAsync();

        // Assert
        _retryServiceMock.Verify(r => r.ShouldRetryAsync(job, failedExecution), Times.Once);
    }
}
