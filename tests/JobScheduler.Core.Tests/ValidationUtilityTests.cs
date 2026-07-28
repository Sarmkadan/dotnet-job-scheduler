using System;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Utilities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class ValidationUtilityTests
{
    [Fact]
    public void ValidateJobName_HappyPath_ReturnsValid()
    {
        var result = ValidationUtility.ValidateJobName("Job_123-ABC");
        Assert.True(result.IsValid);
        Assert.Equal(string.Empty, result.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateJobName_NullOrWhiteSpace_ReturnsInvalid(string? name)
    {
        var result = ValidationUtility.ValidateJobName(name);
        Assert.False(result.IsValid);
        Assert.Equal("Job name is required", result.Message);
    }

    [Fact]
    public void ValidateCronExpression_HappyPath_ReturnsValid()
    {
        // Every 5 minutes
        var result = ValidationUtility.ValidateCronExpression("*/5 * * * *");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCronExpression_InvalidField_ReturnsInvalid()
    {
        var result = ValidationUtility.ValidateCronExpression("61 * * * *"); // minute out of range
        Assert.False(result.IsValid);
        Assert.Contains("minute", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHandlerType_HappyPath_ReturnsValid()
    {
        var result = ValidationUtility.ValidateHandlerType("MyNamespace.MyHandler, MyAssembly");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateHandlerType_InvalidFormat_ReturnsInvalid()
    {
        var result = ValidationUtility.ValidateHandlerType("InvalidHandlerFormat");
        Assert.False(result.IsValid);
        Assert.Equal("Handler type must be in format 'Namespace.Type, AssemblyName'", result.Message);
    }

    [Fact]
    public void ValidateJobConfiguration_HappyPath_ReturnsValid()
    {
        var job = new Job
        {
            MaxRetries = 3,
            RetryBackoffSeconds = 30,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 5
        };

        var result = ValidationUtility.ValidateJobConfiguration(job);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateJobConfiguration_InvalidValues_ReturnsInvalid()
    {
        var job = new Job
        {
            MaxRetries = -1,               // invalid
            RetryBackoffSeconds = 0,
            ExecutionTimeoutSeconds = 0,
            MaxConcurrentExecutions = 0
        };

        var result = ValidationUtility.ValidateJobConfiguration(job);
        Assert.False(result.IsValid);
        Assert.Equal("Max retries must be between 0 and 100", result.Message);
    }

    [Fact]
    public void ValidateJsonParameters_ValidJson_ReturnsValid()
    {
        var json = "{\"name\":\"test\",\"value\":123}";
        var result = ValidationUtility.ValidateJsonParameters(json);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateJsonParameters_InvalidJson_ReturnsInvalid()
    {
        var json = "{name: test,}";
        var result = ValidationUtility.ValidateJsonParameters(json);
        Assert.False(result.IsValid);
        Assert.Equal("Handler parameters must be valid JSON", result.Message);
    }

    [Fact]
    public void ValidatePagination_HappyPath_ReturnsValid()
    {
        var result = ValidationUtility.ValidatePagination(1, 50);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePagination_InvalidPageNumber_ReturnsInvalid()
    {
        var result = ValidationUtility.ValidatePagination(0, 50);
        Assert.False(result.IsValid);
        Assert.Equal("Page number must be 1 or greater", result.Message);
    }

    [Fact]
    public void ValidateRetryStrategy_HappyPath_ReturnsValid()
    {
        var result = ValidationUtility.ValidateRetryStrategy("Exponential");
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRetryStrategy_InvalidStrategy_ReturnsInvalid()
    {
        var result = ValidationUtility.ValidateRetryStrategy("Random");
        Assert.False(result.IsValid);
        Assert.Contains("Retry strategy must be one of", result.Message);
    }
}
