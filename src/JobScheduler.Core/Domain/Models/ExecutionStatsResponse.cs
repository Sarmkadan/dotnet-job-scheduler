namespace JobScheduler.Core.Domain.Models
{
    /// <summary>
    /// Represents the execution statistics for a job.
    /// </summary>
    public sealed class ExecutionStatsResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the job.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Gets or sets the success rate (as a percentage).
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the average execution time in milliseconds.
        /// </summary>
        public long AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution time in milliseconds.
        /// </summary>
        public long MinExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time in milliseconds.
        /// </summary>
        public long MaxExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last execution.
        /// </summary>
        public DateTime? LastExecutionAt { get; set; }

        /// <summary>
        /// Returns a string representation of the execution statistics.
        /// </summary>
        /// <returns>A string representation of the execution statistics.</returns>
        public override string ToString() => $"ExecutionStatsResponse {{ JobId = {JobId}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, SuccessRate = {SuccessRate}, AverageExecutionTimeMs = {AverageExecutionTimeMs} }}";
    }
}