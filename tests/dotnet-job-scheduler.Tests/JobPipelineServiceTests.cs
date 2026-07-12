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

/// <summary>
/// Provides unit tests for the <see cref="JobPipelineService"/> class.
/// Tests various operations including pipeline creation, retrieval, deletion, and mapping functionality.
/// </summary>
public sealed class JobPipelineServiceTests
{
	/// <summary>
	/// Creates an in-memory database context for testing purposes.
	/// </summary>
	/// <returns>A new <see cref="JobSchedulerContext"/> instance using an in-memory database.</returns>
	private static JobSchedulerContext CreateInMemoryContext()
	{
	var options = new DbContextOptionsBuilder<JobSchedulerContext>()
		.UseInMemoryDatabase(Guid.NewGuid().ToString())
		.Options;
	return new JobSchedulerContext(options);
	}

	/// <summary>
	/// Creates a test job with predefined properties for testing pipeline functionality.
	/// </summary>
	/// <param name="name">The name of the job to create.</param>
	/// <returns>A new <see cref="Job"/> instance with test properties.</returns>
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

	/// <summary>
	/// Creates a <see cref="JobPipelineService"/> instance with mocked dependencies for testing.
	/// </summary>
	/// <returns>A tuple containing the service instance and its database context.</returns>
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

	/// <summary>
	/// Tests that a pipeline is successfully created with valid request parameters.
	/// Verifies that the pipeline and all its steps are persisted correctly in the database.
	/// </summary>
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

	/// <summary>
	/// Tests that creating a pipeline with too few steps throws an <see cref="ArgumentException"/>.
	/// Verifies that the service enforces the minimum step requirement.
	/// </summary>
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

	/// <summary>
	/// Tests that creating a pipeline with an empty name throws an <see cref="ArgumentException"/>.
	/// Verifies that the service validates the pipeline name.
	/// </summary>
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

	/// <summary>
	/// Tests that creating a pipeline with a non-existent job throws a <see cref="JobNotFoundException"/>.
	/// Verifies that the service validates job existence before creating pipeline steps.
	/// </summary>
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

	/// <summary>
	/// Tests that a pipeline can be retrieved by its ID with all steps included.
	/// Verifies that the service correctly loads pipeline data from the database.
	/// </summary>
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

	/// <summary>
	/// Tests that retrieving a non-existent pipeline returns null.
	/// Verifies that the service handles unknown IDs gracefully.
	/// </summary>
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

	/// <summary>
	/// Tests that a pipeline can be deleted by its ID.
	/// Verifies that the service removes the pipeline and all its steps from the database.
	/// </summary>
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

	/// <summary>
	/// Tests that deleting a non-existent pipeline returns false.
	/// Verifies that the service handles unknown IDs gracefully during deletion.
	/// </summary>
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

	/// <summary>
	/// Tests that all pipelines can be retrieved from the database.
	/// Verifies that the service returns all created pipelines.
	/// </summary>
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

	/// <summary>
	/// Tests that a pipeline entity is correctly mapped to a response DTO.
	/// Verifies that all fields are properly transferred from the entity to the response.
	/// </summary>
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