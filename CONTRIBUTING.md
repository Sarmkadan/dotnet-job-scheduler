# Contributing to dotnet-job-scheduler

Thank you for your interest in contributing to the dotnet-job-scheduler project! We welcome contributions from the community and appreciate your help in making this project better.

## Ways to Contribute

- **Report bugs**: Open a GitHub Issue with reproducible steps
- **Suggest features**: Discuss new features in GitHub Discussions before implementing
- **Submit code**: Fix bugs or implement features via Pull Request
- **Improve documentation**: Fix typos, clarify examples, add guides
- **Share examples**: Create example implementations showing real-world usage

## Getting Started

### Prerequisites

- **.NET 10.0** or later ([Download](https://dotnet.microsoft.com/download))
- **Git** for version control
- A GitHub account
- Your preferred C# editor (Visual Studio, VS Code with C# Dev Kit, or Rider)

### Development Setup

1. **Fork the repository**
   ```bash
   # Click "Fork" on GitHub, then:
   git clone https://github.com/YOUR_USERNAME/dotnet-job-scheduler.git
   cd dotnet-job-scheduler
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/my-feature
   # or for bug fixes:
   git checkout -b fix/issue-description
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Format code**
   ```bash
   dotnet format
   ```

## Development Workflow

### Before You Start

- Check existing [Issues](https://github.com/sarmkadan/dotnet-job-scheduler/issues) to avoid duplicate work
- Open a new Issue or Discussion for significant features before coding
- For small fixes (typos, documentation), you can skip this step

### Writing Code

#### Code Style

Follow these conventions to maintain consistency:

- **Naming**: PascalCase for public members, camelCase for private members
  ```csharp
  public class JobScheduler { }      // ✓ Public class
  private int _queueSize;            // ✓ Private field
  public int MaxRetries { get; set; } // ✓ Public property
  ```

- **XML Documentation**: Add summaries to public APIs
  ```csharp
  /// <summary>
  /// Executes all jobs that are due for execution.
  /// </summary>
  /// <returns>List of job executions that were triggered.</returns>
  public async Task<List<JobExecution>> ExecuteDueJobsAsync()
  ```

- **Async/Await**: Use async consistently for I/O operations
  ```csharp
  public async Task<Job> GetJobByIdAsync(int jobId)
  {
      return await _repository.GetByIdAsync(jobId);
  }
  ```

- **Line Length**: Keep lines under 120 characters
- **Method Length**: Prefer methods under 30 lines; extract complexity into smaller methods
- **Indentation**: 4 spaces (configured in .editorconfig)

#### Testing

Write tests for all new functionality:

```csharp
[Fact]
public async Task ExecuteDueJobsAsync_WithValidJob_ExecutesSuccessfully()
{
    // Arrange
    var job = new Job { CronExpression = "0 0 * * *", IsActive = true };
    var scheduler = new JobSchedulerService(_jobRepository, _executorService);

    // Act
    var result = await scheduler.ExecuteDueJobsAsync();

    // Assert
    Assert.NotEmpty(result);
}
```

- Use xUnit for unit tests
- Name tests descriptively: `MethodName_Condition_ExpectedResult`
- Aim for >80% code coverage on new code
- Include both success and failure scenarios

### Committing Changes

Write clear, descriptive commit messages:

```bash
# Good commit messages
git commit -m "Add retry backoff strategy configuration"
git commit -m "Fix NullReferenceException in CronExpressionService"
git commit -m "Update documentation for webhook notifications"

# Avoid vague messages
git commit -m "fix stuff"
git commit -m "update"
```

- First line: 50 characters max, imperative mood ("Add", "Fix", "Update")
- Blank line, then detailed explanation if needed
- Reference relevant Issues: "Fixes #123"

### Creating a Pull Request

1. **Push your branch**
   ```bash
   git push origin feature/my-feature
   ```

2. **Open a Pull Request** with:
   - Clear title: "Fix timeout handling in job executor"
   - Description of changes and why
   - Reference to related Issues if applicable
   - Screenshots for UI changes (if any)
   - Link to any related discussions

3. **PR should**:
   - Address one feature or bug fix
   - Pass all CI/CD checks (build, tests, code style)
   - Include or update tests
   - Not introduce breaking changes without discussion
   - Follow the existing code patterns

4. **Code Review Process**:
   - Maintainers will review within 48-72 hours
   - Address feedback respectfully
   - Small follow-up changes can be added to the same PR
   - Once approved, a maintainer will merge your PR

## Project Structure

```
dotnet-job-scheduler/
├── src/JobScheduler.Core/
│   ├── Configuration/        # DI and settings
│   ├── Controllers/          # API endpoints
│   ├── Data/                 # Database and repositories
│   ├── Domain/               # Entities and models
│   ├── Events/               # Event publishing
│   ├── Exceptions/           # Custom exceptions
│   ├── Extensions/           # Helper extensions
│   ├── Middleware/           # HTTP middleware
│   ├── Services/             # Business logic
│   └── Utilities/            # Utility functions
├── tests/JobScheduler.Core.Tests/  # Unit tests
├── examples/                 # Example implementations
└── docs/                     # Documentation
```

## Important Files

- **`JobSchedulerService.cs`**: Central orchestrator - core scheduling logic
- **`JobExecutorService.cs`**: Job execution engine
- **`CronExpressionService.cs`**: Cron parsing and next-execution calculation
- **`JobSchedulerContext.cs`**: Entity Framework Core context
- **Database migrations**: Auto-applied from EF Core

## Making Breaking Changes

Breaking changes require careful consideration:

1. **Discuss first**: Open an Issue or Discussion for debate
2. **Clear migration path**: Document how to upgrade
3. **Deprecation period**: Use `[Obsolete]` attributes first if possible
4. **Major version bump**: Follows semantic versioning (e.g., 1.0 → 2.0)

## Common Issues & Solutions

**Build fails with missing package**
```bash
dotnet restore
dotnet build
```

**Tests fail locally but pass on CI**
- Ensure .NET 10.0 is installed: `dotnet --version`
- Clear cache: `dotnet clean && dotnet build`

**Code formatting issues**
```bash
dotnet format --verify-no-changes  # Check
dotnet format                        # Fix
```

## Documentation

- **README.md**: Main project overview
- **docs/getting-started.md**: Quick start guide
- **docs/architecture.md**: System architecture
- **docs/api-reference.md**: API documentation
- **docs/deployment.md**: Deployment instructions
- **docs/faq.md**: Frequently asked questions

If adding a feature, update relevant documentation:
- Add usage example to README
- Document new APIs in `docs/api-reference.md`
- Update architecture docs if changing core behavior
- Add FAQ entries for common questions

## Author Attribution

All code contributions retain author attribution in file headers. When contributing to existing files, you're not required to add your name to the header (the project maintainer may note contributions in commit history).

## Licensing

By contributing to this project, you agree that your contributions will be licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Code of Conduct

Please review and follow our [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) to foster an inclusive and respectful community.

## Questions?

- **Bugs**: [Open an Issue](https://github.com/sarmkadan/dotnet-job-scheduler/issues)
- **Discussions**: [GitHub Discussions](https://github.com/sarmkadan/dotnet-job-scheduler/discussions)
- **Email**: For sensitive matters, contact rutova2@gmail.com

---

Thank you for contributing! We look forward to working with you.
