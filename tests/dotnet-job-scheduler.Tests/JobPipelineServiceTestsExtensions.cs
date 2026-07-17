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
/// Extension methods for <see cref="JobPipelineServiceTests"/> that provide reusable test utilities
/// for testing <see cref="JobPipelineService"/> functionality.
/// </summary>
public static class JobPipelineServiceTestsExtensions
{
	/// <summary>
	/// Creates a test service with in-memory database context and mocked dependencies.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="depServiceMock">Optional mock for <see cref="IJobDependencyService"/>. If null, a default mock is created.</param>
	/// <returns>A tuple containing the service and its context.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/></exception>
	public static (JobPipelineService Service, JobSchedulerContext Context) CreateService(
		this JobPipelineServiceTests test,
		Mock<IJobDependencyService>? depServiceMock = null)
	{
		ArgumentNullException.ThrowIfNull(test);

		var ctx = CreateInMemoryContext();
		depServiceMock ??= new Mock<IJobDependencyService>();

		var dependencyService = depServiceMock.Object;

		depServiceMock
			.Setup(d => d.AddDependencyAsync(
				It.IsAny<Guid>(),
				It.IsAny<Guid>(),
				It.IsAny<string?>(),
				It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		depServiceMock
			.Setup(d => d.RemoveDependencyAsync(
				It.IsAny<Guid>(),
				It.IsAny<Guid>(),
				It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		var service = new JobPipelineService(ctx, dependencyService);
		return (service, ctx);
	}

	/// <summary>
	/// Creates a simple job for testing.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="name">The job name.</param>
	/// <returns>A configured <see cref="Job"/> entity.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
	public static Job CreateJob(
		this JobPipelineServiceTests test,
		string name)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(name);

		return new Job
		{
			Id = Guid.NewGuid(),
			Name = name,
			CronExpression = "0 9 * * *",
			HandlerType = "TestApp.TestJob, TestApp",
			MaxRetries = 3,
			ExecutionTimeoutSeconds = 300,
			MaxConcurrentExecutions = 1
		};
	}

	/// <summary>
	/// Creates a test pipeline with the specified name and steps.
	/// </summary>
	/// <param name="service">The pipeline service.</param>
	/// <param name="name">The pipeline name.</param>
	/// <param name="steps">The pipeline steps.</param>
	/// <param name="createdBy">The creator identifier. Defaults to "test-user".</param>
	/// <returns>The created pipeline.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentNullException"><paramref name="steps"/> is <see langword="null"/></exception>
	public static async Task<JobPipeline> CreateTestPipelineAsync(
		this JobPipelineService service,
		string name,
		IReadOnlyList<PipelineStepRequest> steps,
		string createdBy = "test-user")
	{
		ArgumentNullException.ThrowIfNull(service);
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(steps);

		var request = new CreatePipelineRequest
		{
			Name = name,
			Description = "Test pipeline",
			Steps = steps.ToList()
		};

		return await service.CreatePipelineAsync(request, createdBy);
	}

	/// <summary>
	/// Asserts that a pipeline has the expected steps in the correct order.
	/// </summary>
	/// <param name="pipeline">The pipeline to verify.</param>
	/// <param name="expectedStepNames">The expected job names in order.</param>
	/// <exception cref="ArgumentNullException"><paramref name="pipeline"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentNullException"><paramref name="expectedStepNames"/> is <see langword="null"/></exception>
	public static void ShouldHaveStepsInOrder(
		this JobPipeline pipeline,
		IReadOnlyList<string> expectedStepNames)
	{
		ArgumentNullException.ThrowIfNull(pipeline);
		ArgumentNullException.ThrowIfNull(expectedStepNames);

		pipeline.Should().NotBeNull();
		pipeline.Steps.Should().HaveCount(expectedStepNames.Count);

		for (var i = 0; i < expectedStepNames.Count; i++)
		{
			pipeline.Steps[i].StepOrder.Should().Be(i);
			pipeline.Steps[i].Job.Should().NotBeNull();
			pipeline.Steps[i].Job!.Name.Should().Be(expectedStepNames[i]);
		}
	}

	private static JobSchedulerContext CreateInMemoryContext()
	{
		var options = new DbContextOptionsBuilder<JobSchedulerContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new JobSchedulerContext(options);
	}
}