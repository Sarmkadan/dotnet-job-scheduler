using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Exceptions;
using System;

namespace dotnet_job_scheduler.Benchmarks
{
    [MemoryDiagnoser]
    /// <summary>
    /// Benchmarks for measuring the performance of JobSchedulerException creation and usage.
    /// </summary>
    public class JobSchedulerExceptionBenchmarks
    {
        private string _message;
        private string _errorCode;
        private Exception _innerException;

        [Params(10, 100, 1000)]
        public int Count { get; set; }

        /// <summary>
        /// Initializes test data for each benchmark iteration.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _message = "Test exception message";
            _errorCode = "TEST_ERROR_CODE";
            _innerException = new InvalidOperationException("Inner exception for testing");
        }

        /// <summary>
        /// Measures the time to create a JobSchedulerException with message only.
        /// </summary>
        [Benchmark]
        public JobSchedulerException CreateWithMessage()
        {
            return new JobSchedulerException(_message);
        }

        /// <summary>
        /// Measures the time to create a JobSchedulerException with message and error code.
        /// </summary>
        [Benchmark]
        public JobSchedulerException CreateWithMessageAndErrorCode()
        {
            return new JobSchedulerException(_message, _errorCode);
        }

        /// <summary>
        /// Measures the time to create a JobSchedulerException with message and inner exception.
        /// </summary>
        [Benchmark]
        public JobSchedulerException CreateWithMessageAndInnerException()
        {
            return new JobSchedulerException(_message, _innerException);
        }

        /// <summary>
        /// Measures the time to create a JobSchedulerException with message, error code, and inner exception.
        /// </summary>
        [Benchmark]
        public JobSchedulerException CreateWithMessageErrorCodeAndInnerException()
        {
            return new JobSchedulerException(_message, _errorCode, _innerException);
        }

        /// <summary>
        /// Measures the time to call ToString() on a JobSchedulerException with message only.
        /// </summary>
        [Benchmark]
        public string ToString_MessageOnly()
        {
            var ex = new JobSchedulerException(_message);
            return ex.ToString();
        }

        /// <summary>
        /// Measures the time to call ToString() on a JobSchedulerException with message and error code.
        /// </summary>
        [Benchmark]
        public string ToString_MessageAndErrorCode()
        {
            var ex = new JobSchedulerException(_message, _errorCode);
            return ex.ToString();
        }

        /// <summary>
        /// Measures the time to call ToString() on a JobSchedulerException with message and inner exception.
        /// </summary>
        [Benchmark]
        public string ToString_MessageAndInnerException()
        {
            var ex = new JobSchedulerException(_message, _innerException);
            return ex.ToString();
        }

        /// <summary>
        /// Measures the time to call ToString() on a JobSchedulerException with message, error code, and inner exception.
        /// </summary>
        [Benchmark]
        public string ToString_MessageErrorCodeAndInnerException()
        {
            var ex = new JobSchedulerException(_message, _errorCode, _innerException);
            return ex.ToString();
        }

        /// <summary>
        /// Measures the time to create multiple JobSchedulerException instances (as specified by Count).
        /// </summary>
        [Benchmark]
        public void CreateMany_MessageOnly()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new JobSchedulerException(_message);
            }
        }

        /// <summary>
        /// Measures the time to create multiple JobSchedulerException instances with message and error code (as specified by Count).
        /// </summary>
        [Benchmark]
        public void CreateMany_MessageAndErrorCode()
        {
            for (int i = 0; i < Count; i++)
            {
                var ex = new JobSchedulerException(_message, _errorCode);
            }
        }
    }
}