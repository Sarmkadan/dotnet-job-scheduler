#nullable enable
using System;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobDependencyTests
{
    [Fact]
    public void DefaultConstructor_Initializes_Id_And_CreatedAt()
    {
        // Arrange & Act
        var dependency = new JobDependency();

        // Assert
        Assert.NotEqual(Guid.Empty, dependency.Id);
        Assert.True((DateTime.UtcNow - dependency.CreatedAt).TotalSeconds < 5,
            "CreatedAt should be set to a recent UTC time");
    }

    [Fact]
    public void Properties_CanBeSetAndRead_BackToBack()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var dependsOnJobId = Guid.NewGuid();
        var createdAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        const string createdBy = "unit-test";

        var dependency = new JobDependency
        {
            JobId = jobId,
            DependsOnJobId = dependsOnJobId,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };

        // Assert
        Assert.Equal(jobId, dependency.JobId);
        Assert.Equal(dependsOnJobId, dependency.DependsOnJobId);
        Assert.Equal(createdAt, dependency.CreatedAt);
        Assert.Equal(createdBy, dependency.CreatedBy);
    }

    [Fact]
    public void CreatedBy_Allows_Null_And_EmptyString()
    {
        // Null case
        var nullCreatedBy = new JobDependency { CreatedBy = null };
        Assert.Null(nullCreatedBy.CreatedBy);

        // Empty string case
        var emptyCreatedBy = new JobDependency { CreatedBy = string.Empty };
        Assert.Equal(string.Empty, emptyCreatedBy.CreatedBy);
    }

    [Fact]
    public void NavigationProperties_CanBeAssigned_And_Retained()
    {
        // Arrange
        var job = new Job { Id = Guid.NewGuid(), Name = "DependentJob" };
        var dependsOnJob = new Job { Id = Guid.NewGuid(), Name = "PrerequisiteJob" };

        var dependency = new JobDependency
        {
            Job = job,
            DependsOnJob = dependsOnJob,
            JobId = job.Id,
            DependsOnJobId = dependsOnJob.Id
        };

        // Assert
        Assert.Same(job, dependency.Job);
        Assert.Same(dependsOnJob, dependency.DependsOnJob);
        Assert.Equal(job.Id, dependency.JobId);
        Assert.Equal(dependsOnJob.Id, dependency.DependsOnJobId);
    }

    [Fact]
    public void Id_Is_Unique_Across_Instances()
    {
        var first = new JobDependency();
        var second = new JobDependency();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Setting_JobId_And_DependsOnJobId_To_Same_Value_Does_Not_Throw()
    {
        // This is an edge case – the class does not enforce DAG rules.
        var guid = Guid.NewGuid();

        var dependency = new JobDependency
        {
            JobId = guid,
            DependsOnJobId = guid
        };

        Assert.Equal(guid, dependency.JobId);
        Assert.Equal(guid, dependency.DependsOnJobId);
    }
}
