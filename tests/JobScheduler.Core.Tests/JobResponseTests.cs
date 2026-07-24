// tests/JobScheduler.Core.Tests/JobResponseTests.cs
using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobResponseTests
{
    [Fact]
    public void FromJob_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            Description = "A description",
            CronExpression = "0 * * * *",
            TimeZoneId = "Eastern Standard Time",
            Priority = JobPriority.High,
            Status = JobStatus.Running,
            HandlerType = "MyNamespace.MyHandler",
            ExecutionTimeoutSeconds = 120,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            LastExecutedAt = DateTime.UtcNow.AddHours(-1),
            NextExecutionAt = DateTime.UtcNow.AddHours(1),
            TotalExecutions = 5,
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            MaxRetries = 3,
            MaxConcurrentExecutions = 2
        };

        // Act
        var response = JobResponse.FromJob(job);

        // Assert
        Assert.Equal(job.Id, response.Id);
        Assert.Equal(job.Name, response.Name);
        Assert.Equal(job.Description, response.Description);
        Assert.Equal(job.CronExpression, response.CronExpression);
        Assert.Equal(job.TimeZoneId, response.TimeZoneId);
        Assert.Equal(job.Priority.ToString(), response.Priority);
        Assert.Equal(job.Status.ToString(), response.Status);
        Assert.Equal(job.HandlerType, response.HandlerType);
        Assert.Equal(job.ExecutionTimeoutSeconds, response.ExecutionTimeoutSeconds);
        Assert.Equal(job.IsActive, response.IsActive);
        Assert.Equal(job.CreatedAt, response.CreatedAt);
        Assert.Equal(job.UpdatedAt, response.UpdatedAt);
        Assert.Equal(job.LastExecutedAt, response.LastExecutedAt);
        Assert.Equal(job.NextExecutionAt, response.NextExecutionAt);
        Assert.Equal(job.TotalExecutions, response.TotalExecutions);
        Assert.Equal(job.SuccessfulExecutions, response.SuccessfulExecutions);
        Assert.Equal(job.FailedExecutions, response.FailedExecutions);
        Assert.Equal(job.GetSuccessRate(), response.SuccessRate);
        Assert.Equal(job.MaxRetries, response.MaxRetries);
        Assert.Equal(job.MaxConcurrentExecutions, response.MaxConcurrentExecutions);
    }

    [Fact]
    public void FromJob_NullTimeZoneId_MapsNullCorrectly()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "No TZ Job",
            Description = "No time zone",
            CronExpression = "0 * * * *",
            TimeZoneId = null,
            Priority = JobPriority.Low,
            Status = JobStatus.Pending,
            HandlerType = "Handler",
            ExecutionTimeoutSeconds = 60,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var response = JobResponse.FromJob(job);

        // Assert
        Assert.Null(response.TimeZoneId);
    }

    [Fact]
    public void FromJob_NullJob_ThrowsNullReferenceException()
    {
        // Arrange
        Job? job = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => JobResponse.FromJob(job!));
    }

    [Fact]
    public void PropertySetAndGet_ReturnsAssignedValues()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var response = new JobResponse
        {
            Id = guid,
            Name = "Name",
            Description = "Desc",
            CronExpression = "0 * * * *",
            TimeZoneId = "UTC",
            Priority = "Medium",
            Status = "Running",
            HandlerType = "Handler",
            ExecutionTimeoutSeconds = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddMinutes(5),
            LastExecutedAt = DateTime.UtcNow.AddMinutes(-10),
            NextExecutionAt = DateTime.UtcNow.AddMinutes(10),
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2,
            SuccessRate = 0.8,
            MaxRetries = 5,
            MaxConcurrentExecutions = 3
        };

        // Assert
        Assert.Equal(guid, response.Id);
        Assert.Equal("Name", response.Name);
        Assert.Equal("Desc", response.Description);
        Assert.Equal("0 * * * *", response.CronExpression);
        Assert.Equal("UTC", response.TimeZoneId);
        Assert.Equal("Medium", response.Priority);
        Assert.Equal("Running", response.Status);
        Assert.Equal("Handler", response.HandlerType);
        Assert.Equal(30, response.ExecutionTimeoutSeconds);
        Assert.True(response.IsActive);
        Assert.Equal(10, response.TotalExecutions);
        Assert.Equal(8, response.SuccessfulExecutions);
        Assert.Equal(2, response.FailedExecutions);
        Assert.Equal(0.8, response.SuccessRate);
        Assert.Equal(5, response.MaxRetries);
        Assert.Equal(3, response.MaxConcurrentExecutions);
    }
}
