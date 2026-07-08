#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Extensions;
using JobScheduler.Core.Utilities;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures job management operations: slug generation, JSON escaping, and
/// other string processing used throughout the scheduler for job identifiers,
/// descriptions, and audit logging.
/// </summary>
[MemoryDiagnoser]
public sealed class JobManagementBenchmarks
{
    // Job name inputs for slug generation
    private const string SimpleJobName = "Daily Report Generation";
    private const string ComplexJobName = "My_Scheduler--Job Name (v2.1)! Production";
    private const string LongJobName = "A Very Long Job Name That Exceeds The Typical Length For Most Job Scheduler Entries In Production Systems";

    // Job description inputs for JSON escaping
    private const string CleanDescription = "Simple job description without special characters";
    private const string SpecialDescription = "Job description with\nnewlines\tand \"quotes\" and \\path\\to\\file";

    // Job handler type inputs
    private const string SimpleHandler = "MyApp.Jobs.ReportHandler, MyApp";
    private const string ComplexHandler = "MyApp.Jobs.Complex.ReportGeneration.V2.Production.Handler, MyApp.Jobs.Complex";

    [Benchmark(Baseline = true)]
    public string GenerateJobSlug_Simple() => SimpleJobName.ToSlug();

    [Benchmark]
    public string GenerateJobSlug_Complex() => ComplexJobName.ToSlug();

    [Benchmark]
    public string GenerateJobSlug_Long() => LongJobName.ToSlug();

    /// <summary>Fast path: no special characters → returns input with no allocation.</summary>
    [Benchmark]
    public string EscapeJobDescription_Clean() => CleanDescription.JsonEscape();

    /// <summary>Slow path: contains special characters.</summary>
    [Benchmark]
    public string EscapeJobDescription_Special() => SpecialDescription.JsonEscape();

    [Benchmark]
    public string TruncateJobDescription() => LongJobName.Truncate(100);

    [Benchmark]
    public string MaskHandlerType() => ComplexHandler.Mask(4);

    [Benchmark]
    public JobPriority ParseJobPriority_High() => (JobPriority)ParseUtility.ParsePriority("high");

    [Benchmark]
    public JobPriority ParseJobPriority_Normal() => (JobPriority)ParseUtility.ParsePriority("normal");

    [Benchmark]
    public JobPriority ParseJobPriority_Low() => (JobPriority)ParseUtility.ParsePriority("low");

    [Benchmark]
    public JobPriority ParseJobPriority_Default() => (JobPriority)ParseUtility.ParsePriority(null);

    [Benchmark]
    public string CreateJobIdentifier() => $"job-{Guid.NewGuid().ToString()[..8]}";

    [Benchmark]
    public string FormatJobStatus() => $"Job Status: {JobStatus.Scheduled}";
}