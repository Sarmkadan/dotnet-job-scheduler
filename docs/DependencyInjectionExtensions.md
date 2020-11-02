# DependencyInjectionExtensions

Provides extension methods and configuration helpers for integrating the job scheduler into ASP.NET Core applications, including service registration, middleware setup, and database initialization.

## API

### `AddJobScheduler(IServiceCollection services, Action<JobSchedulerOptions>? configure = null)`
Registers the core job scheduler services with the dependency injection container.

- **Parameters**
  - `services`: The `IServiceCollection` to register services with.
  - `configure`: Optional action to configure `JobSchedulerOptions`.

- **Returns**
  - `IServiceCollection`: The same instance for method chaining.

- **Throws**
  - `ArgumentNullException`: If `services` is `null`.

### `UseJobSchedulerMiddleware(IApplicationBuilder app)`
Adds the job scheduler middleware to the HTTP request pipeline.

- **Parameters**
  - `app`: The `IApplicationBuilder` to configure.

- **Returns**
  - `IApplicationBuilder`: The same instance for method chaining.

- **Throws**
  - `ArgumentNullException`: If `app` is `null`.

### `InitializeDatabaseAsync(IServiceProvider serviceProvider)`
Ensures the job scheduler database schema is created and migrated.

- **Parameters**
  - `serviceProvider`: The `IServiceProvider` used to resolve required services.

- **Returns**
  - `Task`: A task representing the asynchronous operation.

- **Throws**
  - `ArgumentNullException`: If `serviceProvider` is `null`.
  - `InvalidOperationException`: If required services are not registered.

### `ValidateSchedulerConfiguration(JobSchedulerOptions options)`
Validates the provided scheduler configuration for correctness and consistency.

- **Parameters**
  - `options`: The `JobSchedulerOptions` instance to validate.

- **Throws**
  - `ArgumentNullException`: If `options` is `null`.
  - `InvalidOperationException`: If validation fails (e.g., invalid timeouts or intervals).

### `ConnectionString`
Gets or sets the connection string used by the job scheduler to persist job state.

- **Type**
  - `string?`

### `MaxConcurrentJobs`
Gets or sets the maximum number of jobs that can run concurrently.

- **Type**
  - `int`

### `DefaultTimeoutSeconds`
Gets or sets the default timeout in seconds for jobs that do not specify a timeout.

- **Type**
  - `int`

### `DefaultMaxRetries`
Gets or sets the default maximum number of retry attempts for failed jobs.

- **Type**
  - `int`

### `DefaultRetryBackoffSeconds`
Gets or sets the default delay in seconds between retry attempts for failed jobs.

- **Type**
  - `int`

### `QueuePollIntervalMs`
Gets or sets the interval in milliseconds at which the scheduler polls the job queue.

- **Type**
  - `int`

### `EnableCleanup`
Gets or sets a value indicating whether periodic cleanup of completed or stale jobs is enabled.

- **Type**
  - `bool`

### `CleanupIntervalMs`
Gets or sets the interval in milliseconds between job cleanup operations.

- **Type**
  - `int`

### `EnableLeaderElection`
Gets or sets a value indicating whether leader election is enabled for clustered deployments.

- **Type**
  - `bool`

### `LeaderElectionInstanceId`
Gets or sets the unique identifier for the current instance in leader election.

- **Type**
  - `string?`

### `LeaderElectionLeaseDurationSeconds`
Gets or sets the lease duration in seconds for leader election.

- **Type**
  - `int`

## Usage

### Registering the Scheduler in `Program.cs`
