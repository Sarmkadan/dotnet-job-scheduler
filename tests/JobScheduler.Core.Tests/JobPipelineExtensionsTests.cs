// tests/JobScheduler.Core.Tests/JobPipelineExtensionsTests.cs
using System;
using System.Collections.Generic;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobPipelineExtensionsTests
{
    [Fact]
    public void IsValidForExecution_ReturnsTrue_WhenPipelineIsActiveAndHasSteps()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            IsActive = true,
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep(),
                new JobPipelineStep()
            }
        };

        // Act
        var result = pipeline.IsValidForExecution();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidForExecution_ReturnsFalse_WhenPipelineIsInactive()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            IsActive = false,
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep(),
                new JobPipelineStep()
            }
        };

        // Act
        var result = pipeline.IsValidForExecution();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidForExecution_ReturnsFalse_WhenPipelineHasNoSteps()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            IsActive = true,
            Steps = new List<JobPipelineStep>()
        };

        // Act
        var result = pipeline.IsValidForExecution();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSteps_ReturnsStepsInPipeline()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep(),
                new JobPipelineStep()
            }
        };

        // Act
        var steps = pipeline.GetSteps();

        // Assert
        Assert.Equal(2, steps.Count);
    }

    [Fact]
    public void HasStopOnFailureStep_ReturnsTrue_WhenPipelineHasStopOnFailureStep()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep { StopOnFailure = true },
                new JobPipelineStep()
            }
        };

        // Act
        var result = pipeline.HasStopOnFailureStep();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasStopOnFailureStep_ReturnsFalse_WhenPipelineHasNoStopOnFailureStep()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep(),
                new JobPipelineStep()
            }
        };

        // Act
        var result = pipeline.HasStopOnFailureStep();

        // Assert
        Assert.False(result);
    }
}
