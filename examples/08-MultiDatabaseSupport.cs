public sealed class SimpleJobHandler : IJobHandler
{
    private readonly ILogger<SimpleJobHandler> _logger;

    public SimpleJobHandler(ILogger<SimpleJobHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        _logger.LogInformation("Job executing on database: {Database}", job.Name);
        await Task.Delay(100, cancellationToken);
        return "Execution completed";
    }
