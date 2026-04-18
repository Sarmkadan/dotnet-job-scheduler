# Changelog

All notable changes to dotnet-job-scheduler are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-03-14

### Added
- Add cron expression builder UI with next-run preview calendar
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

---

## [1.1.0] - 2025-10-13

### Added
- Add job dependency graph with DAG validation
- Performance benchmarks with BenchmarkDotNet
- Improved input validation and error messages

### Fixed
- Edge case handling for null/empty inputs
- Resource cleanup in disposal paths
- Thread safety improvements

### Changed
- Optimized hot paths with Span<T> and object pooling
- Better exception messages with parameter details

---

## [1.0.0] - 2025-09-15

### Added
- **Dashboard API**: Aggregated statistics and system-wide metrics endpoints
- **Health Check Endpoint**: `/api/health` for readiness and liveness probes
- **Docker Support**: `Dockerfile` and `docker-compose.yml` for containerized deployment
- **Kubernetes-Ready**: YAML configurations and deployment documentation
- **CI/CD Pipeline**: GitHub Actions workflows for build, test, and NuGet publish
- **Rate Limiting Middleware**: Per-IP request throttling to prevent API abuse
- **Execution History Cleanup**: Automatic retention policy with configurable TTL
- **Slack Notifications**: Direct Slack webhook integration for job alerts
- **Webhook Notifications**: Post-execution HTTP webhooks for external system integration
- **Per-Job Concurrency Limits**: `MaxConcurrentExecutions` field on `Job` entity
- **NuGet Package**: Published as `Zaiets.dotnet.job.scheduler` with full metadata
- **Comprehensive Documentation**: Getting started, architecture, API reference, deployment, and FAQ guides
- **Example Projects**: Eight complete example applications covering all major features

### Changed
- `JobSchedulerSettings` marked stable; all properties documented with XML comments
- `ExecutionStatisticsService` now computes 7-day and 30-day trend windows
- Improved structured logging throughout with consistent log levels

### Fixed
- `ConcurrencyManager` race condition when multiple scheduler instances start simultaneously
- `DbContext` lifetime scope leak in long-running background service loops
- Graceful cancellation for jobs that exceed `ExecutionTimeoutSeconds`

---

## [0.9.0] - 2025-08-18

### Added
- `PerformanceMonitor` service: tracks CPU and memory usage per execution
- `AuditLogger` service: immutable audit trail of all job state changes
- `CacheService`: in-memory caching for job definitions and cron calculations
- `GlobalExceptionMiddleware`: structured error responses with correlation IDs
- `LoggingMiddleware`: request/response logging with timing
- `CsvExportFormatter`: export execution history as CSV

### Changed
- `ExecutionRepository` queries optimised with compiled EF Core expressions
- `ScheduleService` now validates cron expression on job creation
- Connection pool settings tuned for high-throughput scenarios

### Fixed
- Off-by-one in next-execution calculation for monthly cron expressions
- `ExecutionMetrics` aggregation returning stale data after cache expiry

---

## [0.8.0] - 2025-07-28

### Added
- `DashboardController`: summary stats (total jobs, running, success rate, 24 h executions)
- `HealthController`: lightweight health check compatible with ASP.NET Core health-check middleware
- `ExecutionsController`: paginated query and detail endpoints for execution history
- `SlackNotificationService`: configurable Slack alerts on job completion, failure, and suspension
- `WebhookNotificationService`: POST callbacks to external URLs after each execution
- `EventPublisher` / `IEventPublisher`: internal event bus for job lifecycle hooks

### Changed
- `JobsController` response models migrated from entity types to dedicated `JobResponse` / `ExecutionResponse` DTOs
- `BaseController` extracted with shared action-result helpers

---

## [0.7.0] - 2025-07-07

### Added
- `ExternalApiClient`: reusable HTTP client with retry and timeout for job handlers calling external APIs
- `RateLimitMiddleware`: sliding-window rate limiter configurable via `appsettings.json`
- `JobScheduleHistory` entity: records every schedule evaluation for debugging and auditing
- `ExecutionMetrics` entity: stores aggregated statistics per job

### Changed
- `JobExecutorService` now collects `MemoryUsageMb` and `CpuUsagePercent` per execution
- Retry delays recalculated to use `RetryBackoffSeconds` as the base for exponential strategy

### Fixed
- Jobs with `MaxConcurrentExecutions = 1` could still start a second execution during node restart

---

## [0.6.0] - 2025-06-16

### Added
- `JobsController`: full CRUD REST API for job definitions
- `CreateJobRequest` / `JobResponse` / `ExecutionResponse` view models
- Pagination and filtering support on list endpoints (`PageSize`, `PageNumber`, `SortBy`)
- `HttpContextExtensions` and `StringExtensions` helper utilities
- `CollectionExtensions`: batch-processing helpers for execution dispatch

### Changed
- `JobSchedulerService` exposes `SuspendJobAsync` / `ResumeJobAsync` on the public API
- `IJobRepository` extended with `QueryAsync(JobQuery)` for filtered paging

---

## [0.5.0] - 2025-05-26

### Added
- `ExecutionStatisticsService`: computes success rate, average duration, p95/p99 latency per job
- `SchedulerStatistics` aggregate returned by `GetSchedulerStatisticsAsync()`
- `JobSchedulerContext` migrations: added indices on `Status`, `NextExecutionTime`, `JobId`
- `ValidationUtility` and `ParseUtility` helpers used throughout the service layer

### Changed
- `JobRepository.GetDueJobsAsync()` now orders by `Priority` descending before `NextExecutionTime`
- `RetryService` logs each attempt with structured fields for easier debugging

### Fixed
- `GetSchedulerStatisticsAsync` returning incorrect `RunningExecutions` count under load

---

## [0.4.0] - 2025-05-05

### Added
- `ConcurrencyManager`: enforces `MaxConcurrentJobs` (global) and `MaxConcurrentExecutions` (per-job)
- `ConcurrencyException`: thrown when limits are exceeded
- `RetryService`: exponential, linear, and fixed backoff strategies
- `RetryPolicy` entity and `JobPriorityEnum` / `ExecutionStatusEnum` constants
- `JobNotFoundException`, `JobValidationException`, `CronExpressionException` exception hierarchy

### Changed
- `JobSchedulerService` wraps `ConcurrencyManager` before dispatching to `JobExecutorService`
- `JobExecutorService` captures `ExecutedAt`, `CompletedAt`, and `Duration` on every execution

### Fixed
- `ExecuteDueJobsAsync` executing the same job twice when two background service ticks overlapped

---

## [0.3.0] - 2025-04-14

### Added
- `RetryService` skeleton with configurable `MaxRetries` and `RetryBackoffSeconds` per job
- `JobExecution` entity with `RetryAttempt` tracking
- `ExecutionRepository` and `IExecutionRepository` for execution history persistence
- `Repository<T>` generic base repository implementing `IRepository<T>`
- `TimeUtility` and `DateTimeExtensions` for UTC normalisation

### Changed
- `Job` entity extended with `MaxRetries`, `RetryBackoffSeconds`, `ExecutionTimeoutSeconds`
- `JobSchedulerService.ExecuteDueJobsAsync` now persists `JobExecution` records

---

## [0.2.0] - 2025-03-24

### Added
- Entity Framework Core integration with `JobSchedulerContext`
- `Job` and `JobExecution` domain entities
- `IJobRepository` / `JobRepository` with async CRUD operations
- `JobSchedulerService`: `CreateJobAsync`, `UpdateJobAsync`, `DeleteJobAsync`, `GetActiveJobsAsync`
- `JobExecutorService`: executes handlers resolved from `IServiceProvider` with timeout support
- `DependencyInjectionExtensions.AddJobScheduler()` for one-call service registration
- `JobSchedulerSettings` with full configuration surface

### Changed
- `CronExpressionService` switched from custom parser to `NCronTab` library for correctness

---

## [0.1.0] - 2025-03-03

### Added
- Initial project scaffold: `JobScheduler.Core` class library targeting .NET 10
- `CronExpressionService`: parses POSIX cron expressions and calculates next execution times
- `Job` entity skeleton with `Name`, `CronExpression`, `IsActive`, `NextExecutionTime`
- `JobStatus` and `JobPriority` enumerations
- `JobSchedulerException` base exception
- `dotnet-job-scheduler.sln` solution file with test projects wired up
- `xUnit` test projects: `JobScheduler.Core.Tests` and `dotnet-job-scheduler.Tests`

---

## Roadmap

### Version 1.1.0 (Q4 2025)
- [ ] Webhook retry mechanism with exponential backoff
- [ ] Job dependencies and simple workflow chaining
- [ ] Advanced filtering and full-text search on job names

### Version 1.2.0 (Q1 2026)
- [ ] Multi-tenant support
- [ ] Real-time WebSocket updates for the dashboard
- [ ] Job templates and reusability

### Version 2.0.0 (2026) - Released
- [x] Docker hardening: non-root user, health checks, port 8080
- [x] Migration documentation
- [ ] Distributed tracing with OpenTelemetry
- [ ] Advanced analytics and anomaly detection
- [ ] Cloud-native optimisations for Azure / AWS

---

## Version History

| Version | Release Date | .NET Target | Status |
|---------|--------------|-------------|--------|
| 2.0.0   | 2026-03-14   | 10.0        | Current |
| 1.1.0   | 2025-10-13   | 10.0        | Stable  |
| 1.0.0   | 2025-09-15   | 10.0        | Stable  |
| 0.9.0   | 2025-08-18   | 10.0        | Stable  |
| 0.8.0   | 2025-07-28   | 10.0        | Stable  |
| 0.7.0   | 2025-07-07   | 10.0        | Stable  |
| 0.6.0   | 2025-06-16   | 10.0        | Stable  |
| 0.5.0   | 2025-05-26   | 10.0        | Stable  |
| 0.4.0   | 2025-05-05   | 10.0        | Stable  |
| 0.3.0   | 2025-04-14   | 10.0        | Legacy  |
| 0.2.0   | 2025-03-24   | 10.0        | Legacy  |
| 0.1.0   | 2025-03-03   | 10.0        | Legacy  |

---

## Contributing

See [README.md](README.md#contributing) for contribution guidelines.

---

## License

MIT License - Copyright (c) 2025-2026 Vladyslav Zaiets
