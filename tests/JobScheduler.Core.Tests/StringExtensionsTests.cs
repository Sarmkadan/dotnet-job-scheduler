#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Extensions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class StringExtensionsTests
{
    [Fact]
    public void ToSha256_WithValidInput_ReturnsHash()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var hash = input.ToSha256();

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void ToSha256_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => input!.ToSha256());
    }

    [Fact]
    public void Truncate_WithValidInput_ReturnsTruncatedString()
    {
        // Arrange
        var input = "Hello, World!";
        var maxLength = 5;

        // Act
        var truncated = input.Truncate(maxLength);

        // Assert
        Assert.NotNull(truncated);
        Assert.True(truncated.Length <= maxLength);
    }

    [Fact]
    public void ToSlug_WithValidInput_ReturnsSlug()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var slug = input.ToSlug();

        // Assert
        Assert.NotNull(slug);
        Assert.NotEmpty(slug);
    }

    [Fact]
    public void IsValidGuid_WithValidGuid_ReturnsTrue()
    {
        // Arrange
        var input = "12345678-1234-1234-1234-123456789abc";

        // Act
        var isValid = input.IsValidGuid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidEmail_WithValidEmail_ReturnsTrue()
    {
        // Arrange
        var input = "test@example.com";

        // Act
        var isValid = input.IsValidEmail();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Repeat_WithValidInput_ReturnsRepeatedString()
    {
        // Arrange
        var input = "Hello";
        var count = 3;

        // Act
        var repeated = input.Repeat(count);

        // Assert
        Assert.NotNull(repeated);
        Assert.Equal(count * input.Length, repeated.Length);
    }

    [Fact]
    public void Mask_WithValidInput_ReturnsMaskedString()
    {
        // Arrange
        var input = "password123";

        // Act
        var masked = input.Mask();

        // Assert
        Assert.NotNull(masked);
        Assert.NotEmpty(masked);
    }

    [Fact]
    public void ToList_WithValidInput_ReturnsList()
    {
        // Arrange
        var input = "item1,item2,item3";

        // Act
        var list = input.ToList();

        // Assert
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public void IsAlphanumericWithUnderscore_WithValidInput_ReturnsTrue()
    {
        // Arrange
        var input = "Hello_World";

        // Act
        var isValid = input.IsAlphanumericWithUnderscore();

        // Assert
        Assert.True(isValid);
    }
}
