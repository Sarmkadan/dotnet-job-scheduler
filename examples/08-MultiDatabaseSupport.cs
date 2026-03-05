#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Services;

/// <summary>
/// Example 8: Multi-Database Support
///
/// Demonstrates how dotnet-job-scheduler supports multiple database providers
/// (SQL Server, PostgreSQL, MySQL, SQLite) through Entity Framework Core.
/// </summary>

public sealed class SimpleJobHandler : IJobHandler
{
    private readonly ILogger<SimpleJobHandler> _logger;

    public SimpleJobHandler(ILogger<SimpleJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job executing on database: {Database}", job.Name);
        await Task.Delay(100, cancellationToken);
        return "Execution completed";
    }
}

public sealed class MultiDatabaseSupportExample
{
    /// Demonstrates scheduler with different database providers
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Multi-Database Support Example ===\n");

        // Demonstrate different database configurations
        await RunWithSQLite();
        Console.WriteLine("\n" + new string('=', 50) + "\n");
        await RunWithInMemory();
    }

    private static async Task RunWithSQLite()
    {
        Console.WriteLine("--- SQLite Configuration ---\n");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        // SQLite configuration
        services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=scheduler_sqlite.db";
            options.MaxConcurrentJobs = 5;
            options.DefaultTimeoutSeconds = 60;
        });

        // Optional: Configure EF Core explicitly for SQLite
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseSqlite("Data Source=scheduler_sqlite.db");
        });

        services.AddScoped<SimpleJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            Console.WriteLine("Database Information:");
            Console.WriteLine($"Provider: SQLite");
            Console.WriteLine($"Database: scheduler_sqlite.db");
            Console.WriteLine($"Connection: Data Source=scheduler_sqlite.db\n");

            var job = new Job
            {
                Name = "SQLiteTest",
                Description = "Job running on SQLite",
                CronExpression = "* * * * *",
                HandlerType = typeof(SimpleJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                ExecutionTimeoutSeconds = 60
            };

            var createdJob = await schedulerService.CreateJobAsync(job, "example");
            Console.WriteLine($"Created job: {createdJob.Name} (ID: {createdJob.Id})");

            var executions = await schedulerService.ExecuteDueJobsAsync();
            Console.WriteLine($"Executions: {executions.Count}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    private static async Task RunWithInMemory()
    {
        Console.WriteLine("--- In-Memory Database Configuration ---\n");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        // In-memory configuration (useful for testing)
        services.AddJobScheduler(options =>
        {
            options.ConnectionString = ":memory:";
            options.MaxConcurrentJobs = 5;
            options.DefaultTimeoutSeconds = 60;
        });

        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseInMemoryDatabase("scheduler-test");
        });

        services.AddScoped<SimpleJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            Console.WriteLine("Database Information:");
            Console.WriteLine($"Provider: In-Memory");
            Console.WriteLine($"Database: scheduler-test");
            Console.WriteLine($"Connection: :memory: (data not persisted)\n");

            var job = new Job
            {
                Name = "InMemoryTest",
                Description = "Job running in-memory",
                CronExpression = "* * * * *",
                HandlerType = typeof(SimpleJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 0,
                ExecutionTimeoutSeconds = 60
            };

            var createdJob = await schedulerService.CreateJobAsync(job, "example");
            Console.WriteLine($"Created job: {createdJob.Name} (ID: {createdJob.Id})");

            var executions = await schedulerService.ExecuteDueJobsAsync();
            Console.WriteLine($"Executions: {executions.Count}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }
}

/// <summary>
/// Configuration examples for different databases
/// </summary>
public static class DatabaseConfigurationExamples
{
    /// <summary>
    /// SQL Server configuration example
    /// </summary>
    public static void ConfigureSqlServer(IServiceCollection services)
    {
        services.AddJobScheduler(options =>
        {
            // SQL Server connection string
            options.ConnectionString = "Server=localhost;Database=JobScheduler;Integrated Security=true;";
            options.MaxConcurrentJobs = 20;
            options.DefaultTimeoutSeconds = 600;
        });

        // Optional: Configure EF Core for SQL Server
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseSqlServer(
                "Server=localhost;Database=JobScheduler;Integrated Security=true;",
                sqlOptions => sqlOptions.EnableRetryOnFailure()
            );
        });
    }

    /// <summary>
    /// PostgreSQL configuration example
    /// </summary>
    public static void ConfigurePostgreSQL(IServiceCollection services)
    {
        services.AddJobScheduler(options =>
        {
            // PostgreSQL connection string
            options.ConnectionString = "Host=localhost;Database=job_scheduler;Username=postgres;Password=password;";
            options.MaxConcurrentJobs = 20;
            options.DefaultTimeoutSeconds = 600;
        });

        // Optional: Configure EF Core for PostgreSQL
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseNpgsql(
                "Host=localhost;Database=job_scheduler;Username=postgres;Password=password;",
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
            );
        });
    }

    /// <summary>
    /// MySQL configuration example
    /// </summary>
    public static void ConfigureMySQL(IServiceCollection services)
    {
        services.AddJobScheduler(options =>
        {
            // MySQL connection string
            options.ConnectionString = "Server=localhost;Database=job_scheduler;User=root;Password=password;";
            options.MaxConcurrentJobs = 20;
            options.DefaultTimeoutSeconds = 600;
        });

        // Optional: Configure EF Core for MySQL
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseMySql(
                "Server=localhost;Database=job_scheduler;User=root;Password=password;",
                ServerVersion.AutoDetect("Server=localhost;Database=job_scheduler;User=root;Password=password;")
            );
        });
    }

    /// <summary>
    /// SQLite configuration example
    /// </summary>
    public static void ConfigureSQLite(IServiceCollection services)
    {
        services.AddJobScheduler(options =>
        {
            // SQLite connection string
            options.ConnectionString = "Data Source=scheduler.db";
            options.MaxConcurrentJobs = 10; // SQLite works better with lower concurrency
            options.DefaultTimeoutSeconds = 600;
        });

        // Optional: Configure EF Core for SQLite
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseSqlite("Data Source=scheduler.db");
        });
    }

    /// <summary>
    /// Oracle configuration example
    /// </summary>
    public static void ConfigureOracle(IServiceCollection services)
    {
        services.AddJobScheduler(options =>
        {
            // Oracle connection string
            options.ConnectionString = "Data Source=localhost:1521/ORCL;User Id=scheduler;Password=password;";
            options.MaxConcurrentJobs = 20;
            options.DefaultTimeoutSeconds = 600;
        });

        // Optional: Configure EF Core for Oracle
        services.AddDbContext<JobSchedulerContext>(options =>
        {
            options.UseOracle("Data Source=localhost:1521/ORCL;User Id=scheduler;Password=password;");
        });
    }
}

/// <summary>
/// Database migration examples
/// </summary>
public static class DatabaseMigrationExamples
{
    /// <summary>
    /// Example: Migrating from SQLite to SQL Server
    /// </summary>
    public static async Task MigrateSQLiteToSqlServer()
    {
        Console.WriteLine("Example: Migrating from SQLite to SQL Server");
        Console.WriteLine("============================================\n");

        // 1. Backup SQLite database
        Console.WriteLine("1. Backup existing SQLite database");
        Console.WriteLine("   cp scheduler.db scheduler_backup.db\n");

        // 2. Export schema from old database
        Console.WriteLine("2. Generate EF Core migration script from SQLite");
        Console.WriteLine("   dotnet ef migrations script -o migration_to_sqlserver.sql\n");

        // 3. Create new database connection
        Console.WriteLine("3. Update connection string to SQL Server");
        Console.WriteLine("   Server=sqlserver;Database=JobScheduler;...\n");

        // 4. Apply migrations
        Console.WriteLine("4. Apply migrations to new database");
        Console.WriteLine("   dotnet ef database update\n");

        // 5. Verify data
        Console.WriteLine("5. Verify data integrity in new database\n");
    }
}
