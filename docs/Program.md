# Program.cs Documentation

## Service Registration Order

The `CreateHostBuilder` method configures the host and services in the following order:

1. **Logging Configuration**
   - Clears any existing logging providers
   - Adds the console logging provider
   - Sets the minimum log level to `Information`

2. **JobScheduler Services Registration**
   - Calls `services.AddJobScheduler(options)` with the following hardcoded configuration:
     - ConnectionString: `"Data Source=scheduler.db"`
     - MaxConcurrentJobs: `10`
     - DefaultTimeoutSeconds: `300`
     - QueuePollIntervalMs: `5000`

3. **Hosted Service Registration**
   - Registers `SchedulerHostedService` as a hosted service via `services.AddHostedService<SchedulerHostedService>()`

## Middleware Pipeline

This application is a .NET Generic Host worker service, not an ASP.NET Core web application. Therefore, there is no HTTP middleware pipeline. Instead, the application uses a background service (`SchedulerHostedService`) that implements the core scheduling logic.

## SchedulerHostedService Class

The `SchedulerHostedService` class inherits from `BackgroundService` and manages the scheduler lifecycle:

### Startup (ExecuteAsync method)
- Logs "Scheduler hosted service started"
- Enters a loop that continues until a cancellation token is triggered:
  1. Creates a service scope
  2. Resolves `JobSchedulerService` from the service provider
  3. Executes due jobs via `ExecuteDueJobsAsync`
  4. Processes job retries via `ProcessRetriesAsync`
  5. If there were executions or retries, logs scheduler statistics (total jobs, running executions, total executions, success rate)
  6. Waits for 5 seconds before the next iteration (respecting cancellation token)

### Shutdown
- When cancellation is requested:
  - Logs "Scheduler hosted service stopping - initiating graceful shutdown"
  - Calls `GracefulShutdownAsync` to allow in-flight jobs to complete
- The `GracefulShutdownAsync` method:
  - Logs initiation of graceful shutdown with a 30-second timeout
  - Waits for the timeout period (or until cancellation)
  - Logs completion of the graceful shutdown period
  - Does not interrupt the hosted service termination process

### Error Handling
- Exceptions in the execution loop are logged but not rethrown, allowing the scheduler to continue operating
- OperationCanceledException (graceful shutdown) is handled separately to enable clean termination

## Configuration Sources and Environment Variables

The application uses the standard .NET Generic Host configuration system via `Host.CreateDefaultBuilder(args)`, which provides:

### Configuration Sources (in order of precedence):
1. Command-line arguments
2. Environment variables (with prefix `ASPNETCORE_` and `DOTNET_` filtered out)
3. User secrets (when in Development environment)
4. appsettings.json
5. appsettings.{Environment}.json
6. Environment variables (without filtering)
7. Command-line arguments (again)

### JobScheduler Configuration
Despite the general configuration system, the `JobSchedulerOptions` are currently configured with hardcoded values in `Program.cs`:
- ConnectionString: `"Data Source=scheduler.db"`
- MaxConcurrentJobs: `10`
- DefaultTimeoutSeconds: `300`
- QueuePollIntervalMs: `5000`

To make these configurable via environment variables or configuration files, the `AddJobScheduler` method would need to bind options from the configuration system (not currently implemented in this version).

### Environment Variables That Could Be Used
If the `AddJobScheduler` method were updated to bind from configuration, the following environment variables could override the defaults:
- `JOBSCHEDULER_CONNECTIONSTRING`
- `JOBSCHEDULER_MAXCONCURRENTJOBS`
- `JOBSCHEDULER_DEFAULTTIMEOUTSECONDS`
- `JOBSCHEDULER_QUEUEPOLLINTERVALMS`

Or via JSON configuration in `appsettings.json`:
```json
{
  "JobScheduler": {
    "ConnectionString": "Data Source=scheduler.db",
    "MaxConcurrentJobs": 10,
    "DefaultTimeoutSeconds": 300,
    "QueuePollIntervalMs": 5000
  }
}
```