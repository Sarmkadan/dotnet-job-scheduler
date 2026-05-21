// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using JobScheduler.Core.Extensions;

namespace DotnetJobScheduler.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void ToSha256_EmptyString_ReturnsEmpty()
    {
        "".ToSha256().Should().BeEmpty();
    }

    [Fact]
    public void ToSha256_NullString_ReturnsEmpty()
    {
        string? value = null;
        value!.ToSha256().Should().BeEmpty();
    }

    [Fact]
    public void ToSha256_ValidString_ReturnsBase64Hash()
    {
        var hash = "test".ToSha256();
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("test");
    }

    [Fact]
    public void ToSha256_SameInput_ProducesSameHash()
    {
        var hash1 = "consistent".ToSha256();
        var hash2 = "consistent".ToSha256();
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ToSha256_DifferentInput_ProducesDifferentHash()
    {
        var hash1 = "input1".ToSha256();
        var hash2 = "input2".ToSha256();
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Truncate_NullInput_ReturnsNull()
    {
        string? value = null;
        value!.Truncate(10).Should().BeNull();
    }

    [Fact]
    public void Truncate_ShortString_ReturnsOriginal()
    {
        "short".Truncate(20).Should().Be("short");
    }

    [Fact]
    public void Truncate_LongString_TruncatesWithEllipsis()
    {
        var result = "a very long string here".Truncate(10);
        result.Should().EndWith("...");
    }

    [Fact]
    public void Truncate_WithoutEllipsis_TruncatesCleanly()
    {
        var result = "a very long string".Truncate(5, addEllipsis: false);
        result.Should().HaveLength(5);
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void Truncate_ExactLength_ReturnsOriginal()
    {
        "exact".Truncate(5).Should().Be("exact");
    }

    [Fact]
    public void ToSlug_EmptyInput_ReturnsEmpty()
    {
        "".ToSlug().Should().BeEmpty();
    }

    [Fact]
    public void ToSlug_NullInput_ReturnsEmpty()
    {
        string? value = null;
        value!.ToSlug().Should().BeEmpty();
    }

    [Fact]
    public void ToSlug_SpacesConvertedToDashes()
    {
        "My Job Name".ToSlug().Should().Be("my-job-name");
    }
}
