// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Extensions;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures string manipulation operations used throughout the scheduler:
/// slug generation for job identifiers, JSON escaping for parameter payloads,
/// truncation for log output, and credential masking for audit entries.
/// </summary>
[MemoryDiagnoser]
public class StringProcessingBenchmarks
{
    // ToSlug inputs
    private const string ShortName   = "Daily Report Job";
    private const string ComplexName = "My_Scheduler--Job  Name (v2.1)! Production";
    private const string LongName    = "A Very Long Job Name That Exceeds The Typical Length For Most Job Scheduler Entries In Production Systems With Multiple Words";

    // JsonEscape inputs
    private const string CleanPayload   = "SimpleJobPayloadWithoutAnySpecialCharacters";
    private const string SpecialPayload = "Line1\nLine2\twith \"quoted\" value and \\path\\to\\file";

    // Truncate/Mask inputs
    private const string ApiKey     = "sk-prod-abc123def456ghi789jkl012mno345pqr";
    private const string LongString = "This is a very long description that will definitely exceed the truncation limit of 50 characters";

    [Benchmark(Baseline = true)]
    public string ToSlug_Short() => ShortName.ToSlug();

    [Benchmark]
    public string ToSlug_Complex() => ComplexName.ToSlug();

    [Benchmark]
    public string ToSlug_Long() => LongName.ToSlug();

    /// <summary>Fast path: no special characters → returns input with no allocation.</summary>
    [Benchmark]
    public string JsonEscape_Clean() => CleanPayload.JsonEscape();

    /// <summary>Slow path: contains newline, tab, quotes, backslash.</summary>
    [Benchmark]
    public string JsonEscape_Special() => SpecialPayload.JsonEscape();

    [Benchmark]
    public string Truncate_Needed() => LongString.Truncate(50);

    [Benchmark]
    public string Truncate_NoOp() => ShortName.Truncate(200);

    [Benchmark]
    public string Mask_ApiKey() => ApiKey.Mask(4);
}
