// tests/JobScheduler.Core.Tests/CreatePipelineRequestExtensionsTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CreatePipelineRequestExtensionsTests
{
    private CreatePipelineRequest CreateValidRequest()
    {
        return new CreatePipelineRequest
        {
            Name = "TestPipeline",
            Description = null,
            Steps = new List<PipelineStepRequest>()
        };
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenNameAndStepsArePresent()
    {
        // Arrange
        var request = new CreatePipelineRequest
        {
            Name = "Pipeline",
            Steps = new List<PipelineStepRequest>
            {
                new PipelineStepRequest { JobId = Guid.NewGuid() }
            }
        };

        // Act
        var result = request.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenNameIsMissing()
    {
        // Arrange
        var request = new CreatePipelineRequest
        {
            Name = "   ",
            Steps = new List<PipelineStepRequest>
            {
                new PipelineStepRequest { JobId = Guid.NewGuid() }
            }
        };

        // Act
        var result = request.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenStepsAreNullOrEmpty()
    {
        // Arrange - null steps
        var requestNull = new CreatePipelineRequest
        {
            Name = "Pipeline",
            Steps = null!
        };

        // Arrange - empty steps
        var requestEmpty = new CreatePipelineRequest
        {
            Name = "Pipeline",
            Steps = new List<PipelineStepRequest>()
        };

        // Act
        var resultNull = requestNull.IsValid();
        var resultEmpty = requestEmpty.IsValid();

        // Assert
        Assert.False(resultNull);
        Assert.False(resultEmpty);
    }

    [Fact]
    public void IsValid_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        CreatePipelineRequest? request = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.IsValid());
    }

    [Fact]
    public void AddStep_AppendsStepAndReturnsSameInstance()
    {
        // Arrange
        var request = CreateValidRequest();
        var jobId = Guid.NewGuid();

        // Act
        var returned = request.AddStep(jobId, stopOnFailure: false);

        // Assert
        Assert.Same(request, returned);
        Assert.Single(request.Steps);
        var step = request.Steps.First();
        Assert.Equal(jobId, step.JobId);
        Assert.False(step.StopOnFailure);
    }

    [Fact]
    public void AddStep_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        CreatePipelineRequest? request = null;
        var jobId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.AddStep(jobId));
    }

    [Fact]
    public void AddSteps_AppendsAllStepsAndReturnsSameInstance()
    {
        // Arrange
        var request = CreateValidRequest();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var returned = request.AddSteps(ids, stopOnFailure: true);

        // Assert
        Assert.Same(request, returned);
        Assert.Equal(3, request.Steps.Count);
        foreach (var id in ids)
        {
            Assert.Contains(request.Steps, s => s.JobId == id && s.StopOnFailure);
        }
    }

    [Fact]
    public void AddSteps_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        CreatePipelineRequest? request = null;
        var ids = new[] { Guid.NewGuid() };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.AddSteps(ids));
    }

    [Fact]
    public void AddSteps_ThrowsArgumentNullException_WhenJobIdsIsNull()
    {
        // Arrange
        var request = CreateValidRequest();
        IEnumerable<Guid>? ids = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request.AddSteps(ids!));
    }

    [Fact]
    public void SetDescriptionIfEmpty_SetsWhenEmpty()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var returned = request.SetDescriptionIfEmpty("New description");

        // Assert
        Assert.Same(request, returned);
        Assert.Equal("New description", request.Description);
    }

    [Fact]
    public void SetDescriptionIfEmpty_DoesNotOverwrite_WhenAlreadySet()
    {
        // Arrange
        var request = new CreatePipelineRequest
        {
            Name = "Pipeline",
            Description = "Existing",
            Steps = new List<PipelineStepRequest>()
        };

        // Act
        var returned = request.SetDescriptionIfEmpty("Ignored");

        // Assert
        Assert.Same(request, returned);
        Assert.Equal("Existing", request.Description);
    }

    [Fact]
    public void SetDescriptionIfEmpty_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        CreatePipelineRequest? request = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.SetDescriptionIfEmpty("desc"));
    }

    [Fact]
    public void SetDescriptionIfEmpty_ThrowsArgumentNullException_WhenDescriptionIsNull()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request.SetDescriptionIfEmpty(null!));
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new CreatePipelineRequest
        {
            Name = "Original",
            Description = "Desc",
            Steps = new List<PipelineStepRequest>
            {
                new PipelineStepRequest { JobId = Guid.NewGuid(), StopOnFailure = true },
                new PipelineStepRequest { JobId = Guid.NewGuid(), StopOnFailure = false }
            }
        };

        // Act
        var clone = original.Clone();

        // Modify original after cloning
        original.Name = "Modified";
        original.Steps[0].StopOnFailure = false;

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal("Original", clone.Name);
        Assert.Equal("Desc", clone.Description);
        Assert.Equal(2, clone.Steps.Count);
        Assert.True(clone.Steps[0].StopOnFailure); // unchanged
        Assert.False(clone.Steps[1].StopOnFailure);
    }

    [Fact]
    public void Clone_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        CreatePipelineRequest? request = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.Clone());
    }
}
