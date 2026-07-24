// tests/JobScheduler.Core.Tests/CreateJobRequestTests.cs
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CreateJobRequestTests
{
    [Fact]
    public void CreateJobRequest_DefaultValues_ReturnsExpectedDefaults()
    {
        // Arrange & Act
        var request = new CreateJobRequest();

        // Assert
        Assert.Equal(string.Empty, request.Name);
        Assert.Null(request.Description);
        Assert.Equal(string.Empty, request.CronExpression);
        Assert.Null(request.TimeZoneId);
        Assert.Equal(string.Empty, request.HandlerType);
        Assert.Null(request.HandlerParameters);
        Assert.Equal(JobPriority.Normal, request.Priority);
        Assert.Equal(1, request.MaxConcurrentExecutions);
        Assert.Equal(SchedulerConstants.DefaultMaxRetries, request.MaxRetries);
        Assert.Equal(SchedulerConstants.DefaultRetryBackoffSeconds, request.RetryBackoffSeconds);
        Assert.Equal(SchedulerConstants.DefaultExecutionTimeoutSeconds, request.ExecutionTimeoutSeconds);
        Assert.True(request.IsActive);
    }

    [Fact]
    public void CreateJobRequest_SetProperties_StoresValuesCorrectly()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            Description = "Test description",
            CronExpression = "0 0 * * *",
            TimeZoneId = "America/New_York",
            HandlerType = "TestHandler",
            HandlerParameters = "{\"param\":\"value\"}",
            Priority = JobPriority.High,
            MaxConcurrentExecutions = 5,
            MaxRetries = 10,
            RetryBackoffSeconds = 30,
            ExecutionTimeoutSeconds = 600,
            IsActive = false
        };

        // Assert
        Assert.Equal("Test Job", request.Name);
        Assert.Equal("Test description", request.Description);
        Assert.Equal("0 0 * * *", request.CronExpression);
        Assert.Equal("America/New_York", request.TimeZoneId);
        Assert.Equal("TestHandler", request.HandlerType);
        Assert.Equal("{\"param\":\"value\"}", request.HandlerParameters);
        Assert.Equal(JobPriority.High, request.Priority);
        Assert.Equal(5, request.MaxConcurrentExecutions);
        Assert.Equal(10, request.MaxRetries);
        Assert.Equal(30, request.RetryBackoffSeconds);
        Assert.Equal(600, request.ExecutionTimeoutSeconds);
        Assert.False(request.IsActive);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenAllRequiredFieldsAreValid()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalse_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = invalidName!,
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalse_WhenCronExpressionIsInvalid(string? invalidCron)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = invalidCron!,
            HandlerType = "TestHandler"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalse_WhenHandlerTypeIsInvalid(string? invalidHandler)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = invalidHandler!
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IsValid_ReturnsFalse_WhenMaxRetriesIsNegative(int invalidRetries)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            MaxRetries = invalidRetries
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IsValid_ReturnsFalse_WhenExecutionTimeoutSecondsIsInvalid(int invalidTimeout)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = invalidTimeout
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IsValid_ReturnsFalse_WhenMaxConcurrentExecutionsIsInvalid(int invalidConcurrent)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            MaxConcurrentExecutions = invalidConcurrent
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ToJob_ReturnsCorrectJobEntity_WithAllProperties()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            Description = "Test description",
            CronExpression = "0 0 * * *",
            TimeZoneId = "America/New_York",
            HandlerType = "TestHandler",
            HandlerParameters = "{\"param\":\"value\"}",
            Priority = JobPriority.Critical,
            MaxConcurrentExecutions = 5,
            MaxRetries = 10,
            RetryBackoffSeconds = 30,
            ExecutionTimeoutSeconds = 600,
            IsActive = false
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal("Test Job", job.Name);
        Assert.Equal("Test description", job.Description);
        Assert.Equal("0 0 * * *", job.CronExpression);
        Assert.Equal("America/New_York", job.TimeZoneId);
        Assert.Equal("TestHandler", job.HandlerType);
        Assert.Equal("{\"param\":\"value\"}", job.HandlerParameters);
        Assert.Equal(JobPriority.Critical, job.Priority);
        Assert.Equal(5, job.MaxConcurrentExecutions);
        Assert.Equal(10, job.MaxRetries);
        Assert.Equal(30, job.RetryBackoffSeconds);
        Assert.Equal(600, job.ExecutionTimeoutSeconds);
        Assert.False(job.IsActive);
    }

    [Fact]
    public void ToJob_ReturnsCorrectJobEntity_WithNullDescription()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            Description = null
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(string.Empty, job.Description);
    }

    [Fact]
    public void ToJob_ReturnsCorrectJobEntity_WithNullTimeZoneId()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            TimeZoneId = null
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Null(job.TimeZoneId);
    }

    [Fact]
    public void ToJob_ReturnsCorrectJobEntity_WithNullHandlerParameters()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            HandlerParameters = null
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Null(job.HandlerParameters);
    }

    [Fact]
    public void ToJob_ReturnsCorrectJobEntity_WithDefaultValues()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler"
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(JobPriority.Normal, job.Priority);
        Assert.Equal(1, job.MaxConcurrentExecutions);
        Assert.Equal(SchedulerConstants.DefaultMaxRetries, job.MaxRetries);
        Assert.Equal(SchedulerConstants.DefaultRetryBackoffSeconds, job.RetryBackoffSeconds);
        Assert.Equal(SchedulerConstants.DefaultExecutionTimeoutSeconds, job.ExecutionTimeoutSeconds);
        Assert.True(job.IsActive);
    }

    [Theory]
    [InlineData(JobPriority.Low)]
    [InlineData(JobPriority.Normal)]
    [InlineData(JobPriority.High)]
    [InlineData(JobPriority.Critical)]
    public void ToJob_ReturnsCorrectPriority_ForAllPriorityLevels(JobPriority priority)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            Priority = priority
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(priority, job.Priority);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    public void ToJob_ReturnsCorrectMaxConcurrentExecutions(int maxConcurrent, int expected)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            MaxConcurrentExecutions = maxConcurrent
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(expected, job.MaxConcurrentExecutions);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    public void ToJob_ReturnsCorrectMaxRetries(int maxRetries, int expected)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            MaxRetries = maxRetries
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(expected, job.MaxRetries);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(30, 30)]
    [InlineData(3600, 3600)]
    public void ToJob_ReturnsCorrectRetryBackoffSeconds(int retryBackoff, int expected)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            RetryBackoffSeconds = retryBackoff
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(expected, job.RetryBackoffSeconds);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(300, 300)]
    [InlineData(86400, 86400)]
    public void ToJob_ReturnsCorrectExecutionTimeoutSeconds(int timeoutSeconds, int expected)
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Test Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = timeoutSeconds
        };

        // Act
        var job = request.ToJob();

        // Assert
        Assert.Equal(expected, job.ExecutionTimeoutSeconds);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            Description = null,
            TimeZoneId = null,
            HandlerParameters = null
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenOptionalFieldsAreEmpty()
    {
        // Arrange
        var request = new CreateJobRequest
        {
            Name = "Valid Job",
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler",
            Description = string.Empty,
            TimeZoneId = string.Empty,
            HandlerParameters = string.Empty
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        Assert.True(isValid);
    }
}