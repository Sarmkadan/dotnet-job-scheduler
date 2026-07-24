using System;
using System.Collections.Generic;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CreatePipelineRequestTests
{
    [Fact]
    public void CreatePipelineRequest_DefaultValues_ReturnsExpectedDefaults()
    {
        var request = new CreatePipelineRequest();

        Assert.Equal(string.Empty, request.Name);
        Assert.Equal(string.Empty, request.Description);
        Assert.NotNull(request.Steps);
        Assert.Empty(request.Steps);
    }

    [Fact]
    public void CreatePipelineRequest_SetProperties_StoresValuesCorrectly()
    {
        var request = new CreatePipelineRequest
        {
            Name = "My Pipeline",
            Description = "Pipeline description",
            Steps = new List<PipelineStepRequest>
            {
                new PipelineStepRequest { JobId = Guid.NewGuid(), StopOnFailure = false }
            }
        };

        Assert.Equal("My Pipeline", request.Name);
        Assert.Equal("Pipeline description", request.Description);
        Assert.Single(request.Steps);
        Assert.False(request.Steps[0].StopOnFailure);
    }

    [Fact]
    public void PipelineStepRequest_DefaultValues()
    {
        var step = new PipelineStepRequest();

        Assert.Equal(Guid.Empty, step.JobId);
        Assert.True(step.StopOnFailure);
    }

    [Fact]
    public void PipelineStepRequest_SetProperties()
    {
        var jobId = Guid.NewGuid();
        var step = new PipelineStepRequest
        {
            JobId = jobId,
            StopOnFailure = false
        };

        Assert.Equal(jobId, step.JobId);
        Assert.False(step.StopOnFailure);
    }

    [Fact]
    public void PipelineResponse_DefaultValues()
    {
        var response = new PipelineResponse();

        Assert.Equal(Guid.Empty, response.Id);
        Assert.Equal(string.Empty, response.Name);
        Assert.Equal(string.Empty, response.Description);
        Assert.False(response.IsActive);
        Assert.Equal(default(DateTime), response.CreatedAt);
        Assert.Null(response.CreatedBy);
        Assert.NotNull(response.Steps);
        Assert.Empty(response.Steps);
    }

    [Fact]
    public void PipelineResponse_SetProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var response = new PipelineResponse
        {
            Id = id,
            Name = "Pipeline",
            Description = "Desc",
            IsActive = true,
            CreatedAt = createdAt,
            CreatedBy = "user",
            Steps = new List<PipelineStepResponse>
            {
                new PipelineStepResponse
                {
                    StepId = Guid.NewGuid(),
                    JobId = Guid.NewGuid(),
                    JobName = "Job",
                    StepOrder = 1,
                    StopOnFailure = true
                }
            }
        };

        Assert.Equal(id, response.Id);
        Assert.Equal("Pipeline", response.Name);
        Assert.Equal("Desc", response.Description);
        Assert.True(response.IsActive);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal("user", response.CreatedBy);
        Assert.Single(response.Steps);
        Assert.Equal(1, response.Steps[0].StepOrder);
    }

    [Fact]
    public void CreatePipelineRequest_StepsCollection_EmptyAndAdd()
    {
        var request = new CreatePipelineRequest();

        Assert.Empty(request.Steps);

        var step = new PipelineStepRequest { JobId = Guid.NewGuid() };
        request.Steps.Add(step);

        Assert.Single(request.Steps);
        Assert.Equal(step, request.Steps[0]);
    }

    [Fact]
    public void PipelineStepRequest_StopOnFailure_DefaultTrue()
    {
        var step = new PipelineStepRequest();

        Assert.True(step.StopOnFailure);
    }
}
