using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobScheduler.Core.Tests;

public class DependencyGraphValidationResultTests
{
    private static JobSchedulerContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new JobSchedulerContext(options);
        return context;
    }

    private static Job CreateJob(string name = "Job") =>
        new Job
        {
            Id = Guid.NewGuid(),
            Name = name,
            // other required properties can be left with defaults if nullable
        };

    [Fact]
    public async Task ValidateGraphAsync_ReturnsValid_WhenNoJobs()
    {
        await using var ctx = CreateContext();
        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);

        var result = await service.ValidateGraphAsync();

        Assert.True(result.IsValid);
        Assert.Empty(result.CycleNodes);
        Assert.Contains("valid DAG", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateGraphAsync_ReturnsValid_WhenAcyclicGraph()
    {
        await using var ctx = CreateContext();

        var jobA = CreateJob("A");
        var jobB = CreateJob("B");
        ctx.Jobs.AddRange(jobA, jobB);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);
        // B is prerequisite of A (A depends on B)
        await service.AddDependencyAsync(jobA.Id, jobB.Id);

        var result = await service.ValidateGraphAsync();

        Assert.True(result.IsValid);
        Assert.Empty(result.CycleNodes);
        Assert.Contains("valid DAG", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddDependencyAsync_ThrowsCyclicDependencyException_WhenCycleWouldBeCreated()
    {
        await using var ctx = CreateContext();

        var jobA = CreateJob("A");
        var jobB = CreateJob("B");
        var jobC = CreateJob("C");
        ctx.Jobs.AddRange(jobA, jobB, jobC);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);
        await service.AddDependencyAsync(jobA.Id, jobB.Id); // A depends on B
        await service.AddDependencyAsync(jobB.Id, jobC.Id); // B depends on C

        var ex = await Assert.ThrowsAsync<CyclicDependencyException>(async () =>
            await service.AddDependencyAsync(jobC.Id, jobA.Id)); // would close the cycle

        Assert.Equal(jobC.Id, ex.JobId);
        Assert.Equal(jobA.Id, ex.DependsOnJobId);
    }

    [Fact]
    public async Task GetTopologicalOrderAsync_ReturnsJobsInDependencyOrder()
    {
        await using var ctx = CreateContext();

        var jobRoot = CreateJob("Root");
        var jobMid = CreateJob("Mid");
        var jobLeaf = CreateJob("Leaf");
        ctx.Jobs.AddRange(jobRoot, jobMid, jobLeaf);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);
        // Leaf depends on Mid, Mid depends on Root
        await service.AddDependencyAsync(jobLeaf.Id, jobMid.Id);
        await service.AddDependencyAsync(jobMid.Id, jobRoot.Id);

        var ordered = await service.GetTopologicalOrderAsync();

        // Expected order: Root, Mid, Leaf (or any order respecting this precedence)
        var indexRoot = ordered.FindIndex(j => j.Id == jobRoot.Id);
        var indexMid = ordered.FindIndex(j => j.Id == jobMid.Id);
        var indexLeaf = ordered.FindIndex(j => j.Id == jobLeaf.Id);

        Assert.True(indexRoot >= 0 && indexMid >= 0 && indexLeaf >= 0);
        Assert.True(indexRoot < indexMid, "Root should appear before Mid");
        Assert.True(indexMid < indexLeaf, "Mid should appear before Leaf");
    }

    [Fact]
    public async Task GetDependenciesAndDependentsAsync_ReturnCorrectRelations()
    {
        await using var ctx = CreateContext();

        var jobParent = CreateJob("Parent");
        var jobChild = CreateJob("Child");
        ctx.Jobs.AddRange(jobParent, jobChild);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);
        await service.AddDependencyAsync(jobChild.Id, jobParent.Id); // Child depends on Parent

        var dependencies = await service.GetDependenciesAsync(jobChild.Id);
        var dependents = await service.GetDependentsAsync(jobParent.Id);

        Assert.Single(dependencies);
        Assert.Equal(jobParent.Id, dependencies.First().Id);

        Assert.Single(dependents);
        Assert.Equal(jobChild.Id, dependents.First().Id);
    }

    [Fact]
    public async Task AddDependencyAsync_ThrowsJobValidationException_WhenSelfDependency()
    {
        await using var ctx = CreateContext();

        var job = CreateJob("Solo");
        ctx.Jobs.Add(job);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);

        var ex = await Assert.ThrowsAsync<JobValidationException>(async () =>
            await service.AddDependencyAsync(job.Id, job.Id));

        Assert.Contains("cannot depend on itself", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveDependencyAsync_RemovesExistingDependency()
    {
        await using var ctx = CreateContext();

        var jobParent = CreateJob("Parent");
        var jobChild = CreateJob("Child");
        ctx.Jobs.AddRange(jobParent, jobChild);
        await ctx.SaveChangesAsync();

        var service = new JobDependencyService(ctx, NullLogger<JobDependencyService>.Instance);
        await service.AddDependencyAsync(jobChild.Id, jobParent.Id);

        // Ensure it exists
        var before = await service.GetDependenciesAsync(jobChild.Id);
        Assert.Single(before);

        await service.RemoveDependencyAsync(jobChild.Id, jobParent.Id);

        var after = await service.GetDependenciesAsync(jobChild.Id);
        Assert.Empty(after);
    }
}
