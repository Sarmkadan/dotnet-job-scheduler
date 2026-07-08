#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures RetryService operations that handle retry logic:
/// - Retry delay calculation (exponential, linear, fixed backoff)
/// - Retry policy validation
/// - Retry attempt tracking
/// - Maximum retry enforcement
/// </summary>
[MemoryDiagnoser]
public sealed class RetryServiceBenchmarks
{
    private RetryService? _retryService;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<IJobRepository>(new MockJobRepository());
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());
        services.AddSingleton<RetryService>();

        var provider = services.BuildServiceProvider();
        _retryService = provider.GetRequiredService<RetryService>();
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_ExponentialBackoff_FirstAttempt()
    {
        var delay = _retryService!.CalculateRetryDelay(0, JobRetryBackoffStrategy.Exponential);
        return delay;
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_ExponentialBackoff_SecondAttempt()
    {
        var delay = _retryService!.CalculateRetryDelay(1, JobRetryBackoffStrategy.Exponential);
        return delay;
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_ExponentialBackoff_TenthAttempt()
    {
        var delay = _retryService!.CalculateRetryDelay(9, JobRetryBackoffStrategy.Exponential);
        return delay;
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_LinearBackoff_FirstAttempt()
    {
        var delay = _retryService!.CalculateRetryDelay(0, JobRetryBackoffStrategy.Linear);
        return delay;
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_LinearBackoff_FifthAttempt()
    {
        var delay = _retryService!.CalculateRetryDelay(4, JobRetryBackoffStrategy.Linear);
        return delay;
    }

    [Benchmark]
    public TimeSpan CalculateRetryDelay_FixedBackoff()
    {
        var delay = _retryService!.CalculateRetryDelay(0, JobRetryBackoffStrategy.Fixed);
        return delay;
    }

    [Benchmark]
    public bool ShouldRetry_WithinMaxRetries()
    {
        var shouldRetry = _retryService!.ShouldRetry(3, 5); // 3 attempts so far, max 5
        return shouldRetry;
    }

    [Benchmark]
    public bool ShouldRetry_ExceededMaxRetries()
    {
        var shouldRetry = _retryService!.ShouldRetry(5, 5); // 5 attempts, max 5
        return shouldRetry;
    }

    [Benchmark]
    public bool ShouldRetry_ZeroMaxRetries()
    {
        var shouldRetry = _retryService!.ShouldRetry(0, 0); // No retries allowed
        return shouldRetry;
    }

    [Benchmark]
    public int CalculateTotalRetryTime_Exponential()
    {
        var totalTime = _retryService!.CalculateTotalRetryTime(5, JobRetryBackoffStrategy.Exponential, baseDelaySeconds: 5);
        return (int)totalTime.TotalSeconds;
    }

    [Benchmark]
    public int CalculateTotalRetryTime_Linear()
    {
        var totalTime = _retryService!.CalculateTotalRetryTime(5, JobRetryBackoffStrategy.Linear, baseDelaySeconds: 5);
        return (int)totalTime.TotalSeconds;
    }

    [Benchmark]
    public int CalculateTotalRetryTime_Fixed()
    {
        var totalTime = _retryService!.CalculateTotalRetryTime(5, JobRetryBackoffStrategy.Fixed, baseDelaySeconds: 10);
        return (int)totalTime.TotalSeconds;
    }

    [Benchmark]
    public string FormatRetryMessage()
    {
        var message = _retryService!.FormatRetryMessage(3, TimeSpan.FromSeconds(15), "test-server");
        return message;
    }
}

/// <summary>
/// Mock RetryPolicy for testing
/// </summary>
internal sealed class MockRetryPolicy : IRetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public JobRetryBackoffStrategy BackoffStrategy { get; set; } = JobRetryBackoffStrategy.Exponential;
    public int BaseDelaySeconds { get; set; } = 5;
}