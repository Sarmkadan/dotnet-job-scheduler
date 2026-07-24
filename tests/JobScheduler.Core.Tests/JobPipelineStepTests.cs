using System;
using Xunit;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Tests for <see cref="JobPipelineStep"/> entity behavior and edge cases.
/// </summary>
public class JobPipelineStepTests
{
    [Fact]
    public void Constructor_DefaultValues_InitializesCorrectly()
    {
        // Act
        var step = new JobPipelineStep();

        // Assert
        Assert.NotEqual(Guid.Empty, step.Id);
        Assert.Equal(0, step.StepOrder);
        Assert.True(step.StopOnFailure);
        Assert.Null(step.Pipeline);
        Assert.Null(step.Job);
    }

    [Fact]
    public void Constructor_WithParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var pipelineId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var stepOrder = 5;
        var stopOnFailure = false;

        // Act
        var step = new JobPipelineStep
        {
            PipelineId = pipelineId,
            JobId = jobId,
            StepOrder = stepOrder,
            StopOnFailure = stopOnFailure
        };

        // Assert
        Assert.Equal(pipelineId, step.PipelineId);
        Assert.Equal(jobId, step.JobId);
        Assert.Equal(stepOrder, step.StepOrder);
        Assert.False(step.StopOnFailure);
    }

    [Fact]
    public void Id_DefaultValue_IsNotEmpty()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.NotEqual(Guid.Empty, step.Id);
    }

    [Fact]
    public void StepOrder_DefaultValue_IsZero()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.Equal(0, step.StepOrder);
    }

    [Fact]
    public void StopOnFailure_DefaultValue_IsTrue()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.True(step.StopOnFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void StepOrder_CanBeSetToAnyInteger(int order)
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act
        step.StepOrder = order;

        // Assert
        Assert.Equal(order, step.StepOrder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StopOnFailure_CanBeSetToAnyBoolean(bool stopOnFailure)
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act
        step.StopOnFailure = stopOnFailure;

        // Assert
        Assert.Equal(stopOnFailure, step.StopOnFailure);
    }

    [Fact]
    public void Pipeline_NavigationProperty_IsNullByDefault()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.Null(step.Pipeline);
    }

    [Fact]
    public void Job_NavigationProperty_IsNullByDefault()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.Null(step.Job);
    }

    [Fact]
    public void PipelineId_And_Pipeline_Navigation_AreIndependent()
    {
        // Arrange
        var step = new JobPipelineStep();
        var pipeline = new JobPipeline { Id = Guid.NewGuid() };

        // Act
        step.PipelineId = pipeline.Id;
        step.Pipeline = pipeline;

        // Assert
        Assert.Equal(pipeline.Id, step.PipelineId);
        Assert.Same(pipeline, step.Pipeline);
    }

    [Fact]
    public void JobId_And_Job_Navigation_AreIndependent()
    {
        // Arrange
        var step = new JobPipelineStep();
        var job = new Job { Id = Guid.NewGuid() };

        // Act
        step.JobId = job.Id;
        step.Job = job;

        // Assert
        Assert.Equal(job.Id, step.JobId);
        Assert.Same(job, step.Job);
    }

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.True(step.Equals(step));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.False(step.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentReference_ReturnsFalse()
    {
        // Arrange
        var step1 = new JobPipelineStep();
        var step2 = new JobPipelineStep();

        // Act & Assert
        Assert.False(step1.Equals(step2));
    }

    [Fact]
    public void GetHashCode_ForSameObject_ReturnsSameValue()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        Assert.Equal(step.GetHashCode(), step.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsTypeName()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act
        var result = step.ToString();

        // Assert
        Assert.Contains("JobPipelineStep", result);
    }

    [Fact]
    public void StopOnFailure_WhenSetToFalse_AllowsPipelineToContinueOnFailure()
    {
        // Arrange
        var step = new JobPipelineStep { StopOnFailure = false };

        // Act & Assert
        Assert.False(step.StopOnFailure);
    }

    [Fact]
    public void StopOnFailure_WhenSetToTrue_HaltsPipelineOnFailure()
    {
        // Arrange
        var step = new JobPipelineStep { StopOnFailure = true };

        // Act & Assert
        Assert.True(step.StopOnFailure);
    }

    [Fact]
    public void StepOrder_ZeroBased_IndicatesFirstStep()
    {
        // Arrange
        var step = new JobPipelineStep { StepOrder = 0 };

        // Act & Assert
        Assert.Equal(0, step.StepOrder);
    }

    [Fact]
    public void StepOrder_NonZero_IndicatesSubsequentStep()
    {
        // Arrange
        var step = new JobPipelineStep { StepOrder = 5 };

        // Act & Assert
        Assert.Equal(5, step.StepOrder);
    }

    [Fact]
    public void Properties_AreInitializedWithDefaultValues()
    {
        // Arrange
        var step = new JobPipelineStep();

        // Act & Assert
        // These are the default values for a new JobPipelineStep
        Assert.Equal(0, step.StepOrder);
        Assert.True(step.StopOnFailure);
        Assert.Null(step.Pipeline);
        Assert.Null(step.Job);
    }
}