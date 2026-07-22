#nullable enable
using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class CyclicDependencyExceptionJsonExtensionsTests
{
    private static readonly Guid _jobId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
    private static readonly Guid _dependsOnId = Guid.Parse("87654321-4321-4321-4321-cba987654321");
    private static readonly Guid _emptyId = Guid.Empty;

    [Fact]
    public void ToJson_WithValidException_ReturnsNonEmptyJsonString()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var json = exception.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
    }

    [Fact]
    public void ToJson_ContainsExpectedProperties()
    {
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);
        var json = exception.ToJson();

        Assert.Contains("jobId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(_jobId.ToString(), json);
        Assert.Contains("dependsOnJobId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(_dependsOnId.ToString(), json);
        Assert.Contains("CYCLIC_DEPENDENCY_DETECTED", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);
        var json = exception.ToJson(indented: true);

        Assert.NotNull(json);
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);
        var json = exception.ToJson(indented: false);

        Assert.NotNull(json);
        Assert.DoesNotContain(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_WithNullException_ThrowsArgumentNullException()
    {
        CyclicDependencyException? exception = null;
        Assert.Throws<ArgumentNullException>(() => exception!.ToJson());
    }

    [Fact]
    public void FromJson_WithEmptyString_ReturnsNull()
    {
        Assert.Null(CyclicDependencyExceptionJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_WithNull_ReturnsNull()
    {
        Assert.Null(CyclicDependencyExceptionJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_WithWhitespaceOnly_ThrowsJsonException()
    {
        Assert.Throws<System.Text.Json.JsonException>(() =>
            CyclicDependencyExceptionJsonExtensions.FromJson("   \t\n  "));
    }

    [Fact]
    public void TryFromJson_WithEmptyString_ReturnsTrueAndNull()
    {
        var success = CyclicDependencyExceptionJsonExtensions.TryFromJson(string.Empty, out var result);
        Assert.True(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WithNull_ReturnsTrueAndNull()
    {
        var success = CyclicDependencyExceptionJsonExtensions.TryFromJson(null!, out var result);
        Assert.True(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WithWhitespaceOnly_ReturnsFalseAndNull()
    {
        var success = CyclicDependencyExceptionJsonExtensions.TryFromJson("  \t\n  ", out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            CyclicDependencyExceptionJsonExtensions.TryFromJson("not a json", out _));
        Assert.Null(exception);
    }

    [Fact]
    public void ToJson_ProducesValidJsonStructure()
    {
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);
        var json = exception.ToJson();

        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
        Assert.Contains("\"jobId\"", json);
        Assert.Contains("\"dependsOnJobId\"", json);
        Assert.Contains("\"errorCode\"", json);
    }

    [Fact]
    public void ToJson_WithInnerException_IncludesMessage()
    {
        var innerException = new InvalidOperationException("Inner error message");
        var exception = new CyclicDependencyException(_jobId, _dependsOnId, innerException);
        var json = exception.ToJson();

        Assert.Contains("Inner error message", json);
    }

    [Fact]
    public void RoundTrip_ToJsonProducesDeserializableOutput()
    {
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);
        var json = exception.ToJson();
        var parsed = System.Text.Json.JsonDocument.Parse(json);

        Assert.NotNull(parsed);
        Assert.True(parsed.RootElement.TryGetProperty("jobId", out _));
        Assert.True(parsed.RootElement.TryGetProperty("dependsOnJobId", out _));
    }
}
