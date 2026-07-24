// tests/JobScheduler.Core.Tests/ExecutionStatsResponseJsonExtensionsTests.cs
using System;
using System.Text.Json;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class ExecutionStatsResponseJsonExtensionsTests
{
    [Fact]
    public void ToJson_ReturnsJsonString_WhenValueIsValid()
    {
        // Arrange
        var response = new ExecutionStatsResponse();

        // Act
        var json = response.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenValueIsNull()
    {
        // Arrange
        ExecutionStatsResponse? response = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response!.ToJson());
    }

    [Fact]
    public void FromJson_ReturnsObject_WhenJsonIsValid()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = ExecutionStatsResponseJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_ReturnsNull_WhenJsonIsWhiteSpace()
    {
        // Arrange
        var json = "   ";

        // Act
        var result = ExecutionStatsResponseJsonExtensions.FromJson(json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ThrowsArgumentException_WhenJsonIsNullOrEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExecutionStatsResponseJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => ExecutionStatsResponseJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_ThrowsJsonException_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() => ExecutionStatsResponseJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void TryFromJson_ReturnsTrueAndValue_WhenJsonIsValid()
    {
        // Arrange
        var json = "{}";

        // Act
        var success = ExecutionStatsResponseJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.True(success);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var success = ExecutionStatsResponseJsonExtensions.TryFromJson(invalidJson, out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_ThrowsArgumentException_WhenJsonIsNullOrEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExecutionStatsResponseJsonExtensions.TryFromJson(null!, out _));
        Assert.Throws<ArgumentException>(() => ExecutionStatsResponseJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
