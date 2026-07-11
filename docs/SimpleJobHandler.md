# SimpleJobHandler

The `SimpleJobHandler` class serves as the base handler for executing background jobs within the `dotnet-job-scheduler` framework. It provides a unified entry point for job execution and utility methods for configuring supported database backends, ensuring consistent environment setup across different storage infrastructures.

## API

### `SimpleJobHandler()`
Initializes a new instance of the `SimpleJobHandler` class.

### `Task<string> ExecuteAsync()`
Executes the primary logic associated with the job.
*   **Returns:** A `Task<string>` representing the job's completion, containing a status message or result indicator.

### `static Task Main()`
The entry point for the job handler application, facilitating automated task startup.

### `static void ConfigureSqlServer()`
Configures the scheduler to use Microsoft SQL Server as the backing store.

### `static void ConfigurePostgreSQL()`
Configures the scheduler to use PostgreSQL as the backing store.

### `static void ConfigureMySQL()`
Configures the scheduler to use MySQL as the backing store.

### `static void ConfigureSQLite()`
Configures the scheduler to use SQLite as the backing store.

### `static void ConfigureOracle()`
Configures the scheduler to use Oracle Database as the backing store.

### `static Task MigrateSQLiteToSqlServer()`
Initiates a database migration process, transferring job data and state from an existing SQLite instance to a target SQL Server instance.
*   **Returns:** A `Task` representing the asynchronous migration operation.

## Usage

### Basic Job Execution
```csharp
using DotNetJobScheduler;

var handler = new SimpleJobHandler();
string result = await handler.ExecuteAsync();
Console.WriteLine($"Job result: {result}");
```

### Configuring Database Provider
```csharp
using DotNetJobScheduler;

// Configure the application to use PostgreSQL
SimpleJobHandler.ConfigurePostgreSQL();

// Optionally perform migration
await SimpleJobHandler.MigrateSQLiteToSqlServer();
```

## Notes

*   **Thread Safety:** While `ExecuteAsync` is designed for asynchronous operations, instances of `SimpleJobHandler` are not inherently thread-safe. Ensure that shared resources or stateful members within custom implementations are properly synchronized if accessed concurrently.
*   **Configuration:** Database configuration methods must be invoked during application startup before any jobs are processed. Invoking these methods after initialization may result in unpredictable behavior or configuration conflicts.
*   **Database Providers:** Supported database backends require the appropriate drivers installed in the host environment. Ensure connection strings are correctly defined in the configuration environment prior to calling configuration methods.
