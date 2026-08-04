using System;
using JobScheduler.Core.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobSchedulerContextJsonExtensionsTests
{
    private static JobSchedulerContext CreateContext()
    {
        // The context only needs to be instantiated; no database provider is required for
        // serialization tests because we never hit the database.
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .Options;

        return new JobSchedulerContext(options);
    }

    [Fact]
    public void ToJson_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        JobSchedulerContext? nullContext = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullContext!.ToJson());
    }

    [Fact]
    public void ToJson_HappyPath_ReturnsNonEmptyJson()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var json = context.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // The default serializer options ignore nulls, so an empty context should serialize to "{}"
        Assert.Equal("{}", json);
    }

    [Fact]
    public void ToJson_Indented_ReturnsPrettyPrintedJson()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var json = context.ToJson(indented: true);

        // Assert
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void FromJson_NullOrWhiteSpace_ReturnsNull()
    {
        // Arrange
        string nullJson = null!;
        string emptyJson = "";
        string whitespaceJson = "   ";

        // Act
        var resultNull = JobSchedulerContextJsonExtensions.FromJson(nullJson);
        var resultEmpty = JobSchedulerContextJsonExtensions.FromJson(emptyJson);
        var resultWhite = JobSchedulerContextJsonExtensions.FromJson(whitespaceJson);

        // Assert
        Assert.Null(resultNull);
        Assert.Null(resultEmpty);
        Assert.Null(resultWhite);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsContext()
    {
        // Arrange
        var original = CreateContext();
        var json = original.ToJson();

        // Act
        var deserialized = JobSchedulerContextJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<JobSchedulerContext>(deserialized);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndContext()
    {
        // Arrange
        var original = CreateContext();
        var json = original.ToJson();

        // Act
        var success = JobSchedulerContextJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.IsType<JobSchedulerContext>(result);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var malformedJson = "{ this is not valid json }";

        // Act
        var success = JobSchedulerContextJsonExtensions.TryFromJson(malformedJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }
}
