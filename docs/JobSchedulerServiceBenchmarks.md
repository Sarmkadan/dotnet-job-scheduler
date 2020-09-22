# JobSchedulerServiceBenchmarks

A benchmarking suite for the `JobSchedulerService` in the `dotnet-job-scheduler` project, designed to measure the performance of core scheduling operations such as job creation, retrieval, execution, and lifecycle management. The benchmarks provide quantitative insights into throughput, latency, and scalability under varying workloads, enabling optimization of the scheduler's critical paths.

## API

### `void Setup()`
Initializes the benchmark environment before each test run. This method configures in-memory dependencies, resets state, and prepares the scheduler for benchmarking. It is called automatically by the benchmarking framework and should not be invoked manually.

### `async Task CreateJob_Valid()`
Benchmarks the creation of a valid job with a well-formed cron expression. Measures the time taken to persist and register a new job in the scheduler. Throws if the job's cron expression is invalid or if persistence fails.

### `async Task CreateJob_InvalidCron()`
Benchmarks the handling of invalid cron expressions during job creation. Measures the time taken to validate input and return an error without persisting the job. Throws if the cron expression is syntactically malformed or unsupported.

### `async Task CreateJob_DuplicateName()`
Benchmarks the detection of duplicate job names during creation. Measures the time taken to check for existing jobs and reject the new one. Throws if a job with the same name already exists in the scheduler.

### `async Task GetScheduledJobsForExecution()`
Benchmarks the retrieval of jobs scheduled for immediate execution. Measures the time taken to query the job queue and filter jobs by their next execution time. Returns an empty collection if no jobs are due.

### `async Task ExecuteDueJobs_EmptyQueue()`
Benchmarks the execution pipeline when no jobs are due. Measures the overhead of checking the queue and invoking the execution logic. Returns without side effects.

### `async Task ExecuteDueJobs_WithJobs()`
Benchmarks the execution of due jobs in a non-empty queue. Measures the time taken to process jobs, invoke their callbacks, and update their state. Throws if any job's execution callback fails.

### `async Task SuspendJob()`
Benchmarks the suspension of an active job. Measures the time taken to update the job's status and prevent future executions. Throws if the job does not exist or is already suspended.

### `async Task ResumeJob()`
Benchmarks the resumption of a suspended job. Measures the time taken to update the job's status and re-enable scheduling. Throws if the job does not exist or is not suspended.

### `async Task GetSchedulerStatistics()`
Benchmarks the retrieval of scheduler statistics, including job counts and execution metrics. Measures the time taken to aggregate and return performance data. Returns default values if no jobs exist.

### `Task<Job?> GetByIdAsync(Guid id)`
Benchmarks the retrieval of a job by its unique identifier. Measures the time taken to query the underlying store and return the job or `null` if not found. Throws if the identifier is malformed or the store is unavailable.

### `Task<IEnumerable<Job>> GetAllAsync()`
Benchmarks the retrieval of all jobs in the scheduler. Measures the time taken to query the store and return the complete collection. Returns an empty collection if no jobs exist.

### `Task<IEnumerable<Job>> FindAsync(Expression<Func<Job, bool>> predicate)`
Benchmarks the retrieval of jobs matching a predicate. Measures the time taken to apply the filter and return the matching subset. Returns an empty collection if no jobs match.

### `Task<Job?> FirstOrDefaultAsync(Expression<Func<Job, bool>> predicate)`
Benchmarks the retrieval of the first job matching a predicate. Measures the time taken to apply the filter and return the first matching job or `null` if none exist. Returns `null` if no jobs match.

### `Task<int> CountAsync()`
Benchmarks the retrieval of the total number of jobs in the scheduler. Measures the time taken to query the store and return the count. Returns zero if no jobs exist.

### `Task AddAsync(Job job)`
Benchmarks the addition of a single job to the scheduler. Measures the time taken to validate, persist, and register the job. Throws if the job is invalid or a duplicate.

### `Task AddRangeAsync(IEnumerable<Job> jobs)`
Benchmarks the batch addition of multiple jobs to the scheduler. Measures the time taken to validate, persist, and register all jobs. Throws if any job is invalid or a duplicate.

### `void Update(Job job)`
Benchmarks the update of a single job's properties. Measures the time taken to validate, persist, and propagate changes. Throws if the job does not exist or is invalid.

### `void UpdateRange(IEnumerable<Job> jobs)`
Benchmarks the batch update of multiple jobs' properties. Measures the time taken to validate, persist, and propagate all changes. Throws if any job does not exist or is invalid.

### `void Remove(Job job)`
Benchmarks the removal of a single job from the scheduler. Measures the time taken to validate, persist, and deregister the job. Throws if the job does not exist.

## Usage

### Example 1: Benchmarking Job Creation
