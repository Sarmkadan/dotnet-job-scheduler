// tests/JobScheduler.Core.Tests/CreateJobRequestJsonExtensionsTests.cs
using System;
using System.Text.Json;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CreateJobRequestJsonExtensionsTests
{
    [Fact]
    public void ToJson_ReturnsJsonString_WhenValueIsValid()
    {
        // Arrange
        var request = new CreateJobRequest();

        // Act
        var json = request.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenValueIsNull()
    {
        // Arrange
        CreateJobRequest? request = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request!.ToJson());
    }

    [Fact]
    public void ToJson_IndentsWhenRequested()
    {
        // Arrange
        var request = new CreateJobRequest();

        // Act
        var compact = request.ToJson(indented: false);
        var indented = request.ToJson(indented: true);

        // Assert
        Assert.NotEqual(compact, indented);
        // Indented JSON should contain a newline character
        Assert.Contains("\n", indented);
    }

    [Fact]
    public void FromJson_ReturnsObject_WhenJsonIsValid()
    {
        // Arrange
        var original = new CreateJobRequest();
        var json = original.ToJson();

        // Act
        var deserialized = CreateJobRequestJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        // Since we don't know the properties, we just ensure the type matches
        Assert.IsType<CreateJobRequest>(deserialized);
    }

    [Fact]
    public void FromJson_ReturnsNull_WhenJsonIsWhiteSpace()
    {
        // Arrange
        var json = "   ";

        // Act
        var result = CreateJobRequestJsonExtensions.FromJson(json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ReturnsNull_WhenJsonIsNull()
    {
        // Act
        var result = CreateJobRequestJsonExtensions.FromJson(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ThrowsJsonException_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<JsonException>(() => CreateJobRequestJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void TryFromJson_ReturnsTrueAndValue_WhenJsonIsValid()
    {
        // Arrange
        var request = new CreateJobRequest();
        var json = request.ToJson();

        // Act
        var success = CreateJobRequestJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.True(success);
        Assert.NotNull(value);
        Assert.IsType<CreateJobRequest>(value);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_WhenJsonIsInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var success = CreateJobRequestJsonExtensions.TryFromJson(invalidJson, out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_WhenJsonIsWhiteSpace()
    {
        // Arrange
        var json = "   ";

        // Act
        var success = CreateJobRequestJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }
}
