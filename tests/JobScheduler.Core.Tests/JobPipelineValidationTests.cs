using System;
using System.Collections.Generic;
using Xunit;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Tests;

public class JobPipelineValidationTests
{
    [Fact]
    public void Validate_HappyPath_ReturnsEmptyList()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Name = "Test Pipeline",
            Description = "Test pipeline description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep
                {
                    Id = Guid.NewGuid(),
                    PipelineId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    StepOrder = 1
                }
            }
        };

        // Act
        var errors = JobPipelineValidation.Validate(pipeline);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void IsValid_HappyPath_ReturnsTrue()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Name = "Test Pipeline",
            Description = "Test pipeline description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep
                {
                    Id = Guid.NewGuid(),
                    PipelineId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    StepOrder = 1
                }
            }
        };

        // Act
        var isValid = JobPipelineValidation.IsValid(pipeline);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void EnsureValid_HappyPath_DoesNotThrow()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Name = "Test Pipeline",
            Description = "Test pipeline description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep
                {
                    Id = Guid.NewGuid(),
                    PipelineId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    StepOrder = 1
                }
            }
        };

        // Act and Assert
        JobPipelineValidation.EnsureValid(pipeline);
    }

    [Fact]
    public void Validate_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => JobPipelineValidation.Validate(null));
    }

    [Fact]
    public void IsValid_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => JobPipelineValidation.IsValid(null));
    }

    [Fact]
    public void EnsureValid_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => JobPipelineValidation.EnsureValid(null));
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        // Arrange
        var pipeline = new JobPipeline
        {
            Name = "",
            Description = "Test pipeline description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "Test User",
            Steps = new List<JobPipelineStep>
            {
                new JobPipelineStep
                {
                    Id = Guid.NewGuid(),
                    PipelineId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    StepOrder = 1
                }
            }
        };

        // Act
        var errors = JobPipelineValidation.Validate(pipeline);

        // Assert
        Assert.Single(errors);
    }
}
