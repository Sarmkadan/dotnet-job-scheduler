namespace JobScheduler.Core.Domain.Models
{
    public sealed class ExecutionStatsResponse
    {
        public Guid JobId { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double SuccessRate { get; set; }
        public long AverageExecutionTimeMs { get; set; }
        public long MinExecutionTimeMs { get; set; }
        public long MaxExecutionTimeMs { get; set; }
        public DateTime? LastExecutionAt { get; set; }

        public override string ToString() => $"ExecutionStatsResponse {{ JobId = {JobId}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, SuccessRate = {SuccessRate}, AverageExecutionTimeMs = {AverageExecutionTimeMs} }}";
    }
}