using System;
using System.Reflection;
using System.Runtime.Serialization;
using JobScheduler.Core.Services;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for <see cref="JobPipelineServiceJsonExtensions"/>.
/// </summary>
public sealed class JobPipelineServiceJsonExtensionsTests
{
    /// <summary>
    /// Creates an uninitialized instance of <see cref="JobPipelineService"/> for serialization tests.
    /// </summary>
    private static JobPipelineService CreateService()
    {
#pragma warning disable SYSLIB0050
        return (JobPipelineService)FormatterServices.GetUninitializedObject(typeof(JobPipelineService));
#pragma warning restore SYSLIB0050
    }

    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        var service = CreateService();

        var json = service.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.DoesNotContain("\n", json); // default is non‑indented
    }

    [Fact]
    public void ToJson_IndentedTrue_ReturnsFormattedJson()
    {
        var service = CreateService();

        var json = service.ToJson(indented: true);

        Assert.False(string.IsNullOrWhiteSpace(json)); // indented serialization produces valid JSON
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        JobPipelineService? nullService = null;

        Assert.Throws<ArgumentNullException>(() => nullService!.ToJson());
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsNull()
    {
        var result = JobPipelineServiceJsonExtensions.FromJson(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JobPipelineServiceJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_ValidJson_ThrowsInvalidOperationException()
    {
        // Even a minimal JSON object cannot be deserialized because the type
        // resolver explicitly throws during object creation.
        var json = "{}";

        Assert.Throws<InvalidOperationException>(() => JobPipelineServiceJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_EmptyString_ReturnsFalse()
    {
        var success = JobPipelineServiceJsonExtensions.TryFromJson(string.Empty, out var value);

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JobPipelineServiceJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_ValidJson_ThrowsInvalidOperationException()
    {
        var json = "{}";

        Assert.Throws<InvalidOperationException>(() => JobPipelineServiceJsonExtensions.TryFromJson(json, out _));
    }
}
