#nullable enable
using System;
using System.Text.Json;
using JobScheduler.Core.Extensions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class StringExtensionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidString_ReturnsQuotedJson()
    {
        // Arrange
        var input = "hello world";

        // Act
        var json = input.ToJson();

        // Assert
        Assert.Equal(JsonSerializer.Serialize(input), json);
        // The result should be a quoted JSON string
        Assert.Equal("\"hello world\"", json);
    }

    [Fact]
    public void ToJson_WithIndentation_ReturnsSameJsonForString()
    {
        // Arrange
        var input = "indented";

        // Act
        var jsonIndented = input.ToJson(indented: true);
        var jsonNormal = input.ToJson();

        // Assert
        // For a simple string, indentation does not change the output
        Assert.Equal(jsonNormal, jsonIndented);
    }

    [Fact]
    public void ToJson_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input!.ToJson());
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsOriginalString()
    {
        // Arrange
        var original = "sample text";
        var json = JsonSerializer.Serialize(original); // "\"sample text\""

        // Act
        var result = StringExtensionsJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(original, result);
    }

    [Fact]
    public void FromJson_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var result = StringExtensionsJsonExtensions.FromJson(json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StringExtensionsJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        var malformed = "not-a-json";

        // Act & Assert
        Assert.Throws<JsonException>(() => StringExtensionsJsonExtensions.FromJson(malformed));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndValue()
    {
        // Arrange
        var original = "valid";
        var json = JsonSerializer.Serialize(original);
        string? value;

        // Act
        var success = StringExtensionsJsonExtensions.TryFromJson(json, out value);

        // Assert
        Assert.True(success);
        Assert.Equal(original, value);
    }

    [Fact]
    public void TryFromJson_WithEmptyOrNull_ReturnsTrueAndNullValue()
    {
        // Empty string case
        string? valueEmpty;
        var successEmpty = StringExtensionsJsonExtensions.TryFromJson(string.Empty, out valueEmpty);
        Assert.True(successEmpty);
        Assert.Null(valueEmpty);

        // Null case
        string? valueNull;
        var successNull = StringExtensionsJsonExtensions.TryFromJson(null, out valueNull);
        Assert.True(successNull);
        Assert.Null(valueNull);
    }

    [Fact]
    public void TryFromJson_WithMalformedJson_ReturnsFalseAndNullValue()
    {
        // Arrange
        var malformed = "bad json";
        string? value;

        // Act
        var success = StringExtensionsJsonExtensions.TryFromJson(malformed, out value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }
}
