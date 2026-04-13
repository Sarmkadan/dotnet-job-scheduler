#nullable enable

using FluentAssertions;
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

public sealed class JobPipelineServiceTests
{
    private static JobSchedulerContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobSchedulerContext(options);
    }

    private static Job CreateJob(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CronExpression = "0 9 * * *",
        HandlerType = "TestApp.TestJob, TestApp",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = 1
    };

    private static (JobPipelineService Service, JobSchedulerContext Context) CreateService()
    {
        var ctx = CreateInMemoryContext();
        var depServiceMock = new Mock<IJobDependencyService>();
        depServiceMock
            .Setup(d => d.AddDependencyAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        depServiceMock
            .Setup(d => d.RemoveDependencyAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new JobPipelineService(ctx, depServiceMock.Object);
        return (service, ctx);
    }

    [Fact]
    public async Task CreatePipelineAsync_WithValidRequest_CreatesPipelineAndDependencies()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var jobA = CreateJob("job-a");
        var jobB = CreateJob("job-b");
        var jobC = CreateJob("job-c");
        ctx.Jobs.AddRange(jobA, jobB, jobC);
        await ctx.SaveChangesAsync();

        var request = new CreatePipelineRequest
        {
            Name = "test-pipeline",
            Description = "Test pipeline",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = jobA.Id },
                new() { JobId = jobB.Id },
                new() { JobId = jobC.Id }
            }
        };

        // Act
        var pipeline = await service.CreatePipelineAsync(request, "admin");

        // Assert
        pipeline.Should().NotBeNull();
        pipeline.Name.Should().Be("test-pipeline");
        pipeline.Steps.Should().HaveCount(3);
        pipeline.CreatedBy.Should().Be("admin");

        var storedPipeline = await ctx.Set<JobPipeline>().Include(p => p.Steps).FirstOrDefaultAsync();
        storedPipeline.Should().NotBeNull();
        storedPipeline!.Steps.Should().HaveCount(3);
        storedPipeline.Steps.Select(s => s.StepOrder).Should().BeEquivalentTo(new[] { 0, 1, 2 });
    }

    [Fact]
    public async Task CreatePipelineAsync_WithTooFewSteps_ThrowsArgumentException()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var jobA = CreateJob("single-job");
        ctx.Jobs.Add(jobA);
        await ctx.SaveChangesAsync();

        var request = new CreatePipelineRequest
        {
            Name = "one-step",
            Steps = new List<PipelineStepRequest> { new() { JobId = jobA.Id } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePipelineAsync(request));
    }

    [Fact]
    public async Task CreatePipelineAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var (service, _) = CreateService();
        var request = new CreatePipelineRequest
        {
            Name = string.Empty,
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = Guid.NewGuid() },
                new() { JobId = Guid.NewGuid() }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePipelineAsync(request));
    }

    [Fact]
    public async Task CreatePipelineAsync_WithNonExistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var existingJob = CreateJob("existing");
        ctx.Jobs.Add(existingJob);
        await ctx.SaveChangesAsync();

        var request = new CreatePipelineRequest
        {
            Name = "broken-pipeline",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = existingJob.Id },
                new() { JobId = Guid.NewGuid() } // does not exist
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(() => service.CreatePipelineAsync(request));
    }

    [Fact]
    public async Task GetPipelineAsync_WithValidId_ReturnsPipelineWithSteps()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var jobA = CreateJob("pa");
        var jobB = CreateJob("pb");
        ctx.Jobs.AddRange(jobA, jobB);
        await ctx.SaveChangesAsync();

        var request = new CreatePipelineRequest
        {
            Name = "fetch-test",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = jobA.Id },
                new() { JobId = jobB.Id }
            }
        };
        var created = await service.CreatePipelineAsync(request);

        // Act
        var fetched = await service.GetPipelineAsync(created.Id);

        // Assert
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("fetch-test");
        fetched.Steps.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPipelineAsync_WithUnknownId_ReturnsNull()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.GetPipelineAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePipelineAsync_WithValidId_RemovesPipeline()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var jobA = CreateJob("del-a");
        var jobB = CreateJob("del-b");
        ctx.Jobs.AddRange(jobA, jobB);
        await ctx.SaveChangesAsync();

        var request = new CreatePipelineRequest
        {
            Name = "to-delete",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = jobA.Id },
                new() { JobId = jobB.Id }
            }
        };
        var created = await service.CreatePipelineAsync(request);

        // Act
        var deleted = await service.DeletePipelineAsync(created.Id);

        // Assert
        deleted.Should().BeTrue();
        var remaining = await ctx.Set<JobPipeline>().CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeletePipelineAsync_WithUnknownId_ReturnsFalse()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.DeletePipelineAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllPipelinesAsync_ReturnsAllPipelines()
    {
        // Arrange
        var (service, ctx) = CreateService();
        var jobs = new[] { CreateJob("ga"), CreateJob("gb"), CreateJob("gc"), CreateJob("gd") };
        ctx.Jobs.AddRange(jobs);
        await ctx.SaveChangesAsync();

        await service.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "pipeline-1",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = jobs[0].Id },
                new() { JobId = jobs[1].Id }
            }
        });
        await service.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "pipeline-2",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = jobs[2].Id },
                new() { JobId = jobs[3].Id }
            }
        });

        // Act
        var all = await service.GetAllPipelinesAsync();

        // Assert
        all.Should().HaveCount(2);
    }

    [Fact]
    public void MapToResponse_WithPipeline_MapsAllFields()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Id = Guid.NewGuid(),
            Name = "map-test",
            Description = "desc",
            IsActive = true,
            CreatedBy = "user",
            Steps = new List<JobPipelineStep>
            {
                new() { Id = Guid.NewGuid(), JobId = Guid.NewGuid(), StepOrder = 0, StopOnFailure = true, Job = new Job { Name = "step-0" } },
                new() { Id = Guid.NewGuid(), JobId = Guid.NewGuid(), StepOrder = 1, StopOnFailure = false, Job = new Job { Name = "step-1" } }
            }
        };

        // Act
        var response = JobPipelineService.MapToResponse(pipeline);

        // Assert
        response.Id.Should().Be(pipeline.Id);
        response.Name.Should().Be("map-test");
        response.Steps.Should().HaveCount(2);
        response.Steps[0].StepOrder.Should().Be(0);
        response.Steps[1].JobName.Should().Be("step-1");
    }
}
