using System;
using System.Collections.Generic;
using Xunit;
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Tests;

public sealed class AuditLoggerValidationTests
{
    private static AuditLogEntry CreateValidEntry()
    {
        return new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "TestEvent",
            Timestamp = DateTime.UtcNow,
            Severity = AuditSeverity.Info,
            Details = "Some details about the event.",
            UserId = "user123",
            EntityId = Guid.NewGuid(),
            EntityType = "TestEntity"
        };
    }

    [Fact]
    public void Validate_ValidEntry_ReturnsEmptyList()
    {
        var entry = CreateValidEntry();

        var problems = entry.Validate();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_InvalidEntry_ReturnsProblems()
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.Empty, // invalid
            EventType = new string('a', 101), // too long
            Timestamp = DateTime.UtcNow.AddMinutes(10), // future too far
            Severity = (AuditSeverity)999, // invalid
            Details = new string('d', 4001), // too long
            UserId = "", // empty
            EntityId = Guid.Empty, // invalid
            EntityType = new string('e', 51) // too long
        };

        var problems = entry.Validate();

        Assert.Equal(8, problems.Count);
        Assert.Contains(problems, p => p.Contains("EventId"));
        Assert.Contains(problems, p => p.Contains("EventType") && p.Contains("100"));
        Assert.Contains(problems, p => p.Contains("Timestamp") && p.Contains("future"));
        Assert.Contains(problems, p => p.Contains("Severity"));
        Assert.Contains(problems, p => p.Contains("Details") && p.Contains("4000"));
        Assert.Contains(problems, p => p.Contains("UserId") && p.Contains("empty"));
        Assert.Contains(problems, p => p.Contains("EntityId") && p.Contains("Empty"));
        Assert.Contains(problems, p => p.Contains("EntityType") && p.Contains("50"));
    }

    [Fact]
    public void IsValid_ValidEntry_ReturnsTrue()
    {
        var entry = CreateValidEntry();

        Assert.True(entry.IsValid());
    }

    [Fact]
    public void IsValid_InvalidEntry_ReturnsFalse()
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.Empty,
            EventType = "E",
            Timestamp = DateTime.UtcNow,
            Severity = AuditSeverity.Debug,
            Details = "D"
        };

        Assert.False(entry.IsValid());
    }

    [Fact]
    public void EnsureValid_ValidEntry_DoesNotThrow()
    {
        var entry = CreateValidEntry();

        var exception = Record.Exception(() => entry.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_InvalidEntry_ThrowsArgumentException()
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.Empty,
            EventType = "",
            Timestamp = DateTime.UtcNow,
            Severity = AuditSeverity.Critical,
            Details = ""
        };

        var ex = Assert.Throws<ArgumentException>(() => entry.EnsureValid());
        Assert.Contains("AuditLogEntry is not valid", ex.Message);
    }

    [Fact]
    public void Validate_Null_ThrowsArgumentNullException()
    {
        AuditLogEntry? entry = null;
        Assert.Throws<ArgumentNullException>(() => entry.Validate());
    }

    [Fact]
    public void IsValid_Null_ThrowsArgumentNullException()
    {
        AuditLogEntry? entry = null;
        Assert.Throws<ArgumentNullException>(() => entry.IsValid());
    }

    [Fact]
    public void EnsureValid_Null_ThrowsArgumentNullException()
    {
        AuditLogEntry? entry = null;
        Assert.Throws<ArgumentNullException>(() => entry.EnsureValid());
    }
}
