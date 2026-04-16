// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Utilities;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures CSV parsing and escaping used by the export formatter and
/// audit log serializer. ParseCsvLine is called once per line during
/// bulk imports; EscapeCsvField is called per-cell during export.
/// </summary>
[MemoryDiagnoser]
public class CsvProcessingBenchmarks
{
    // Typical flat export row (no quoting needed)
    private const string SimpleLine =
        "daily-report,0 9 * * *,active,high,3,300";

    // Row containing fields with embedded commas and quotes
    private const string QuotedLine =
        "\"Daily Report, v2\",\"0 9 * * *\",\"active\",\"high\",\"3\",\"300\"";

    // Wide row — 10 fields — tests field-count scalability
    private const string WideLine =
        "f1,f2,f3,f4,f5,f6,f7,f8,f9,f10";

    private const string PlainField  = "SimpleJobName";
    private const string CommaField  = "Report, Daily";
    private const string QuotedField = "Value \"with\" quotes";

    [Benchmark(Baseline = true)]
    public List<string> ParseCsvLine_Simple() =>
        ParseUtility.ParseCsvLine(SimpleLine);

    [Benchmark]
    public List<string> ParseCsvLine_Quoted() =>
        ParseUtility.ParseCsvLine(QuotedLine);

    [Benchmark]
    public List<string> ParseCsvLine_Wide() =>
        ParseUtility.ParseCsvLine(WideLine);

    /// <summary>Fast path: no special chars → returns original string, no allocation.</summary>
    [Benchmark]
    public string EscapeCsvField_Plain() =>
        ParseUtility.EscapeCsvField(PlainField);

    [Benchmark]
    public string EscapeCsvField_Comma() =>
        ParseUtility.EscapeCsvField(CommaField);

    [Benchmark]
    public string EscapeCsvField_Quotes() =>
        ParseUtility.EscapeCsvField(QuotedField);

    [Benchmark]
    public int ParsePriority_ByName() =>
        ParseUtility.ParsePriority("high");

    [Benchmark]
    public int ParsePriority_Default() =>
        ParseUtility.ParsePriority(null);
}
