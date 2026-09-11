#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Contains unit tests for the <see cref="JobsController"/> class, covering job CRUD operations, lifecycle management, and status updates.
/// </summary>
public sealed class JobsControllerTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly Mock<ILogger<JobsController>> _loggerMock = new();

    private JobsController CreateController()
    {
        var cronService = new CronExpressionService();
        var retryService = new RetryService(_jobRepoMock.Object, _executionRepoMock.Object);
        var concurrencyManager = new ConcurrencyManager(_executionRepoMock.Object);
        var executorService = new JobExecutorService(
            _jobRepoMock.Object,
            _executionRepoMock.Object,
            concurrencyManager);
        var schedulerService = new JobSchedulerService(
            _jobRepoMock.Object,
            _executionRepoMock.Object,
            executorService,
            cronService,
            retryService,
            concurrencyManager,
            null);

        return new JobsController(schedulerService, _loggerMock.Object);
    }

    private static CreateJobRequest CreateValidRequest(string name = "TestJob") => new()
    {
        Name = name,
        CronExpression = "0 * * * *",
        HandlerType = "Test.Handler"
    };

    private static Job CreateJob(Guid id, string name = "TestJob") => new()
    {
        Id = id,
        Name = name,
        CronExpression = "0 * * * *",
        HandlerType = "Test.Handler",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = 1,
        Status = JobStatus.Scheduled,
        IsActive = true
    };

    /// <summary>
    /// Verifies that <see cref="JobsController.CreateJob(CreateJobRequest)"/> returns 201 Created when a valid job request is provided.
    /// </summary>
    [Fact]
    public async Task CreateJob_ReturnsCreated_WhenRequestIsValid()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Job?)null);
        _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<Job>())).Returns(Task.CompletedTask);
        _jobRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.CreateJob(CreateValidRequest());

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(JobsController.GetJob), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues?["id"]);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.CreateJob(CreateJobRequest)"/> returns 400 Bad Request when the request is null.
    /// </summary>
    [Fact]
    public async Task CreateJob_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.CreateJob(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.GetJob(Guid)"/> returns 200 OK when a job exists with the specified ID.
    /// </summary>
    [Fact]
    public async Task GetJob_ReturnsOk_WhenJobExists()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));

        var controller = CreateController();

        // Act
        var result = await controller.GetJob(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<JobResponse>(okResult.Value);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.GetJob(Guid)"/> returns 404 Not Found when no job exists with the specified ID.
    /// </summary>
    [Fact]
    public async Task GetJob_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync((Job?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetJob(jobId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.ListJobs(JobStatus?,int,int)"/> returns 200 OK with a paginated response.
    /// </summary>
    [Fact]
    public async Task ListJobs_ReturnsOk_WithPaginatedResponse()
    {
        // Arrange
        var jobs = new[]
        {
            CreateJob(Guid.NewGuid(), "Job1"),
            CreateJob(Guid.NewGuid(), "Job2")
        };
        _jobRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(jobs);

        var controller = CreateController();

        // Act
        var result = await controller.ListJobs(null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var paginatedResponse = Assert.IsType<PaginatedResponse<JobResponse>>(okResult.Value);
        Assert.Equal(2, paginatedResponse.TotalCount);
        Assert.Equal(2, paginatedResponse.Data.Count);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.UpdateJob(Guid,CreateJobRequest)"/> returns 200 OK when the job is successfully updated.
    /// </summary>
    [Fact]
    public async Task UpdateJob_ReturnsOk_WhenJobIsUpdated()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));
        _jobRepoMock.Setup(r => r.Update(It.IsAny<Job>()));
        _jobRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateJob(jobId, CreateValidRequest("UpdatedJob"));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<JobResponse>(okResult.Value);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.UpdateJob(Guid,CreateJobRequest)"/> returns 404 Not Found when the job does not exist.
    /// </summary>
    [Fact]
    public async Task UpdateJob_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync((Job?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateJob(jobId, CreateValidRequest());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.DeleteJob(Guid)"/> returns 204 No Content when the job is successfully deleted.
    /// </summary>
    [Fact]
    public async Task DeleteJob_ReturnsNoContent_WhenJobIsDeleted()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));
        _jobRepoMock.Setup(r => r.Remove(It.IsAny<Job>()));
        _jobRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteJob(jobId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.DeleteJob(Guid)"/> returns 404 Not Found when the job does not exist.
    /// </summary>
    [Fact]
    public async Task DeleteJob_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync((Job?)null);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteJob(jobId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.SuspendJob(Guid,SuspendJobRequest?)"/> returns 200 OK when the job is successfully suspended.
    /// </summary>
    [Fact]
    public async Task SuspendJob_ReturnsOk_WhenJobIsSuspended()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));
        _jobRepoMock.Setup(r => r.Update(It.IsAny<Job>()));
        _jobRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.SuspendJob(jobId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<JobResponse>(okResult.Value);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.ResumeJob(Guid)"/> returns 200 OK when the job is successfully resumed.
    /// </summary>
    [Fact]
    public async Task ResumeJob_ReturnsOk_WhenJobIsResumed()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));
        _jobRepoMock.Setup(r => r.Update(It.IsAny<Job>()));
        _jobRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.ResumeJob(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<JobResponse>(okResult.Value);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.TriggerJobExecution(Guid)"/> returns 200 OK when job execution is successfully triggered.
    /// </summary>
    [Fact]
    public async Task TriggerJobExecution_ReturnsOk_WhenExecutionIsTriggered()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(jobId)).ReturnsAsync(0);
        _executionRepoMock.Setup(r => r.AddAsync(It.IsAny<JobExecution>())).Returns(Task.CompletedTask);
        _executionRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.TriggerJobExecution(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ExecutionResponse>(okResult.Value);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.TriggerJobExecution(Guid)"/> returns 409 Conflict when job execution cannot be triggered due to concurrency limits.
    /// </summary>
    [Fact]
    public async Task TriggerJobExecution_ReturnsConflict_WhenConcurrencyLimitExceeded()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = CreateJob(jobId);
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(jobId)).ReturnsAsync(2);

        var controller = CreateController();

        // Act
        var result = await controller.TriggerJobExecution(jobId);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    /// <summary>
    /// Verifies that <see cref="JobsController.GetJobExecutionHistory(Guid,int)"/> returns 200 OK with execution history.
    /// </summary>
    [Fact]
    public async Task GetJobExecutionHistory_ReturnsOk_WithExecutionHistory()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var executions = new[]
        {
            new JobExecution { Id = Guid.NewGuid(), JobId = jobId, Status = ExecutionStatus.Success },
            new JobExecution { Id = Guid.NewGuid(), JobId = jobId, Status = ExecutionStatus.Failed }
        };
        _jobRepoMock.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(CreateJob(jobId));
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(jobId)).ReturnsAsync(executions);

        var controller = CreateController();

        // Act
        var result = await controller.GetJobExecutionHistory(jobId, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var executionResponses = Assert.IsAssignableFrom<IEnumerable<ExecutionResponse>>(okResult.Value);
        Assert.Equal(2, executionResponses.Count());
    }
}