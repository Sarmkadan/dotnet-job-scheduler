# CLAUDE.md

## Project overview

`dotnet-job-scheduler` - a .NET 10 distributed job scheduling library/service (cron jobs, retries, pipelines, distributed locks, leader election) with EF Core SQLite persistence and an ASP.NET Core REST API. NuGet package id: `dotnet-job-scheduler`.

## Build

```bash
dotnet restore dotnet-job-scheduler.sln
dotnet build dotnet-job-scheduler.sln --configuration Release   # or: make build
dotnet build dotnet-job-scheduler.sln --configuration Debug     # or: make build-debug
dotnet run --project src/JobScheduler.Core/JobScheduler.Core.csproj   # or: make run
```

Requires .NET SDK 10.x (`net10.0` target, `LangVersion=latest`, nullable + implicit usings enabled). CI (`.github/workflows/ci.yml`) builds Release and runs tests on a matrix of 8.0.x and 10.0.x.

## Test

```bash
dotnet test dotnet-job-scheduler.sln --configuration Release           # all tests
dotnet test tests/JobScheduler.Core.Tests/JobScheduler.Core.Tests.csproj
dotnet test tests/dotnet-job-scheduler.Tests/dotnet-job-scheduler.Tests.csproj
dotnet test dotnet-job-scheduler.sln --filter "FullyQualifiedName~JobSchedulerServiceTests"
make test-coverage   # XPlat Code Coverage, cobertura
make test-watch      # watch mode on JobScheduler.Core.Tests
```

Note: `make test` passes `--no-build`; run `make build` first.

Test conventions:
- Framework: xUnit 2.9 + Moq. Two test projects:
  - `tests/JobScheduler.Core.Tests` - namespace `JobScheduler.Core.Tests`; plain `Assert.*`, one file per source type (`XTests.cs`, `XExtensionsTests.cs`, `XValidationTests.cs`, `XJsonExtensionsTests.cs`).
  - `tests/dotnet-job-scheduler.Tests` - namespace `DotnetJobScheduler.Tests`; FluentAssertions 8 + EF Core InMemory; service/integration/behavioral tests. This assembly name has `InternalsVisibleTo` from Core.
- Test classes are `public sealed`, mocks are `private readonly Mock<T> _xMock = new()` fields, a `CreateService()` factory builds the SUT, `CreateValidJob()`-style static helpers build fixtures.
- Test methods carry `/// <summary>` XML docs like production code.
- Benchmarks (BenchmarkDotNet) live in `benchmarks/dotnet-job-scheduler.Benchmarks`; not part of `dotnet test`.

## Lint / Format

```bash
dotnet format dotnet-job-scheduler.sln                        # make format
dotnet format dotnet-job-scheduler.sln --verify-no-changes    # make format-check
dotnet build dotnet-job-scheduler.sln /p:TreatWarningsAsErrors=true   # make lint
make ci   # restore + build-debug + test + format-check + lint
```

Style is enforced by `.editorconfig`: 4-space indent, LF, Allman braces (`csharp_new_line_before_open_brace = all`), braces required, `using` directives outside namespace with System first, no `this.` qualification, `var` only when type is apparent, expression-bodied members only when single-line. JSON/YAML use 2 spaces.

## Architecture

Single library/host project `src/JobScheduler.Core` (SDK `Microsoft.NET.Sdk.Web`, `GenerateDocumentationFile=true`):

| Directory | Contents |
|---|---|
| `Program.cs` | Entry point. `Host.CreateDefaultBuilder` -> `services.AddJobScheduler(...)` -> `SchedulerHostedService` (`BackgroundService`) owns the single polling loop (`ExecuteDueJobsAsync` + `ProcessRetriesAsync` per scope). |
| `Configuration/` | `DependencyInjectionExtensions.AddJobScheduler()` - the one place all services are registered; `JobSchedulerSettings`, `DotnetJobSchedulerOptions`. |
| `Domain/Entities/` | EF entities: `Job`, `JobExecution`, `JobDependency`, `JobPipeline`, `JobScheduleHistory`, `RetryPolicy`, `ExecutionMetrics`. |
| `Domain/Models/` | API request/response DTOs (`CreateJobRequest`, `JobResponse`, `ExecutionResponse`, ...). |
| `Data/` | `JobSchedulerContext` (EF Core, SQLite) and `Repositories/` (`IRepository<T>`/`Repository<T>` generic base, `IJobRepository`, `IExecutionRepository`). |
| `Services/` | Business logic: `JobSchedulerService` (facade), `JobExecutorService`, `RetryService`, `ConcurrencyManager`, `CronExpressionService` (NCronTab), `JobPipelineService`, `JobDependencyService`, `DistributedJobLockService`, `DatabaseLeaderElectionService`, `CacheService`, `PerformanceMonitor`, `AuditLogger`, `Slack/WebhookNotificationService`. |
| `Controllers/` | ASP.NET Core API: `BaseController` + `Jobs/Executions/Pipelines/History/Dashboard/Health/MetricsController`. |
| `Middleware/` | `GlobalExceptionMiddleware`, `LoggingMiddleware`, `RateLimitMiddleware`. |
| `Exceptions/` | `JobSchedulerException` base with `ErrorCode`; typed subclasses (`JobNotFoundException`, `JobValidationException`, `CronExpressionException`, `ConcurrencyException`, `CyclicDependencyException`, `ExecutionException`). |
| `Events/` | `IEventPublisher`/`EventPublisher`. |
| `Constants/` | Enums (`JobStatus`, `ExecutionStatus`, `JobPriority`, `MisfirePolicy`) + their extension classes, `SchedulerConstants`. |
| `Extensions/`, `Utilities/`, `Formatters/`, `Metrics/`, `Abstractions/` | Helpers (`ITimeProvider` for testable time, `CsvExportFormatter`, `SchedulerMetrics`). |

Other top-level: `examples/` (standalone usage samples, not in the solution), `docs/` (one markdown per public type), `Dockerfile` + `docker-compose.yml` (`make docker-*`), `Makefile` (canonical command list).

Pattern: a type `X.cs` is commonly accompanied by partial/companion files `XExtensions.cs`, `XValidation.cs`, `XJsonExtensions.cs` in the same folder. Follow this split when adding functionality rather than growing the main file.

## Conventions

- Every `.cs` file starts with `#nullable enable` and the author header block (`// Author: Vladyslav Zaiets | https://sarmkadan.com`). File-scoped namespaces (`namespace JobScheduler.Core.X;`).
- Public types and members require `/// <summary>` XML docs (doc file generation is on, so missing docs produce warnings and `make lint` fails). Comments explaining rationale use a `// WHY:` prefix.
- Naming: PascalCase public members, `_camelCase` private fields, `Async` suffix on async methods, interfaces `I*`, enums in `Constants/` with `*Enum.cs` filename but plain type name (e.g. `JobStatus`).
- Classes are `sealed` unless designed for inheritance (exceptions, `BaseController`, `Repository<T>`).
- Argument validation: `ArgumentNullException.ThrowIfNull(x)` / `ArgumentException.ThrowIfNullOrWhiteSpace(x)` at method entry; constructor injection uses `?? throw new ArgumentNullException(nameof(x))`.
- Error handling: throw typed `JobSchedulerException` subclasses from services; `GlobalExceptionMiddleware` maps them to HTTP status (validation/cron -> 400, not found -> 404, concurrency -> 409, everything else -> 500) and returns a JSON error body. Do not catch-and-swallow in services; log with `ILogger<T>` structured templates (`{Method} {Path}`), never string interpolation.
- DI lifetimes: `DbContext`, repositories, and anything depending on them are `Scoped`; stateless services (`CronExpressionService`, `PerformanceMonitor`) are `Singleton`. Never register a service depending on a repository as singleton (captive dependency - see comment on `ConcurrencyManager` in `DependencyInjectionExtensions`). Background loops resolve scoped services via `IServiceProvider.CreateScope()` per iteration.
- Persistence: EF Core with SQLite (`Data Source=scheduler.db`); migrations via `make db-add-migration NAME=...` / `make db-migrate`. Do not commit `scheduler.db*` changes.
- Do not add new manual scheduling loops; `SchedulerHostedService` is the only poller.
- Stray files in the tree (`*.backup`, `*.orig`, `*.rej`, `.aider.*`, `Commit message`, `}`) are leftovers, not conventions; do not extend them.
