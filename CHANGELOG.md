# Changelog

All notable changes to dotnet-job-scheduler are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- **Webhook Notifications**: Post-execution webhooks for external system integration
- **Slack Integration**: Direct Slack notifications for job events
- **Performance Metrics**: Detailed execution time and resource usage tracking
- **Dashboard API**: Comprehensive statistics and metrics endpoints
- **Health Check Endpoint**: Service health and readiness checks
- **Docker Support**: Full containerization with Docker and docker-compose
- **Kubernetes Ready**: YAML configurations for Kubernetes deployment
- **CI/CD Pipeline**: GitHub Actions workflow for automated builds and tests
- **Comprehensive Documentation**: Getting started, architecture, API reference, deployment guides
- **Multiple Examples**: 6 complete example applications demonstrating different use cases
- **Rate Limiting**: API request throttling and rate limiting
- **Execution History Cleanup**: Automatic retention policy enforcement
- **Per-Job Concurrency**: Individual job execution limits
- **Distributed Locking**: Support for multiple scheduler instances

### Changed
- **Database Optimization**: Improved indices for faster queries
- **Query Performance**: Optimized EF Core queries for large datasets
- **Logging**: Enhanced structured logging with Serilog integration
- **Error Messages**: More detailed and actionable error information
- **Retry Logic**: Improved backoff calculation and scheduling

### Fixed
- **Concurrency Issues**: Prevented race conditions in job execution
- **Memory Leaks**: Fixed DbContext disposal in service layer
- **Timeout Handling**: Improved graceful cancellation of timed-out jobs
- **Database Connection**: Better connection pooling and resource management

## [1.1.0] - 2026-04-20

### Added
- **Cron Expression Validation**: Full POSIX cron expression support with validation
- **Status Management**: Complete job lifecycle with suspend/resume
- **Retry Policies**: Exponential, linear, and fixed backoff strategies
- **Concurrency Control**: Global and per-job execution limits
- **Execution Statistics**: Success rates and performance metrics
- **Audit Logging**: Complete audit trail of job state changes
- **Generic Repositories**: Abstracted data access with Entity Framework Core
- **Dependency Injection**: Full Microsoft.Extensions.DependencyInjection support
- **REST API**: Complete job and execution management endpoints
- **Background Service**: Built-in hosted service for job execution

### Changed
- **Architecture**: Refactored to clean architecture with clear layer separation
- **Database Schema**: Added performance indices and constraints
- **API Design**: RESTful endpoints following Microsoft conventions
- **Configuration**: Fluent builder configuration for settings

### Fixed
- **Job Scheduling**: Corrected next execution time calculation for edge cases
- **Status Transitions**: Fixed invalid state transitions
- **Error Handling**: Improved exception handling and logging

## [1.0.0] - 2026-04-01

### Added
- Initial release of dotnet-job-scheduler
- Core scheduling engine with CRON support
- Job execution with configurable timeout
- Retry mechanism with backoff strategies
- Priority-based job queue
- Execution history and metrics
- Entity Framework Core integration
- Microsoft Dependency Injection support
- Async/await throughout codebase

### Features
- **CRON Scheduling**: POSIX cron expression support
- **Priority Queues**: Low, Normal, High, Critical priority levels
- **Automatic Retries**: Configurable retry attempts with backoff
- **Concurrency Control**: Prevent system overload with limits
- **Job Metrics**: Track execution times and success rates
- **Database Agnostic**: Supports SQL Server, PostgreSQL, MySQL, SQLite
- **Flexible Handlers**: Custom job handler implementation support
- **Status Tracking**: Complete job lifecycle management

---

## Upgrade Guide

### From 1.1.x to 1.2.0

1. Update NuGet package:
```bash
dotnet package update DotNet.JobScheduler.Core --version 1.2.0
```

2. Apply database migrations:
```bash
dotnet ef database update
```

3. (Optional) Configure new features:
```csharp
options.EnableWebhookNotifications = true;
options.SlackWebhookUrl = "https://hooks.slack.com/...";
```

### From 1.0.x to 1.1.0

1. Update NuGet package
2. Rebuild project
3. No breaking changes - existing code remains compatible

---

## Known Issues

### Version 1.2.0
- Webhook retries not yet implemented (will add in 1.3.0)
- Slack integration requires valid webhook URL
- Rate limiting applies per IP, not per user

### Version 1.1.0
- Cron expression with seconds field not supported (use minute precision)
- Timezone support limited to Windows timezone names on Linux

---

## Roadmap

### Version 1.3.0 (Q3 2026)
- [ ] Webhook retry mechanism with exponential backoff
- [ ] Job dependencies and workflow support
- [ ] Advanced filtering and search
- [ ] Job duplication prevention
- [ ] Real-time WebSocket updates

### Version 1.4.0 (Q4 2026)
- [ ] Multi-tenant support
- [ ] Custom job scheduling strategies
- [ ] Event sourcing for job history
- [ ] GraphQL API
- [ ] Job templates and reusability

### Version 2.0.0 (2027)
- [ ] Distributed tracing with OpenTelemetry
- [ ] Advanced analytics and reporting
- [ ] Job visualization dashboard
- [ ] Machine learning for anomaly detection
- [ ] Cloud-native optimizations

---

## Contributing

See [README.md](README.md#contributing) for contribution guidelines.

---

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

---

## Version History

| Version | Release Date | .NET Target | Status |
|---------|------------|------------|--------|
| 1.2.0 | 2026-05-04 | 10.0 | Current |
| 1.1.0 | 2026-04-20 | 10.0 | Stable |
| 1.0.0 | 2026-04-01 | 10.0 | Legacy |

---

For more information, visit [https://sarmkadan.com](https://sarmkadan.com) or contact rutova2@gmail.com.
