public sealed class SimpleJobHandler : IJobHandler
{
    private readonly ILogger<SimpleJobHandler> _logger;

    public SimpleJobHandler(ILogger<SimpleJobHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)\n        {\n            try\n            {\n                ArgumentNullException.ThrowIfNull(job);\n                _logger.LogInformation("Job executing on database: {Database}", job.Name);\n                _logger.LogInformation("Starting job execution", );\n                await Task.Delay(100, cancellationToken);\n                _logger.LogInformation("Finished job execution", );\n                return "Execution completed";\n            }\n            catch (Exception ex)\n            {\n                _logger.LogError(ex, "Failed to execute job");\n                throw;\n            }\n        }
    {
        ArgumentNullException.ThrowIfNull(job);
        _logger.LogInformation("Job executing on database: {Database}", job.Name);
        await Task.Delay(100, cancellationToken);
        return "Execution completed";
    }
