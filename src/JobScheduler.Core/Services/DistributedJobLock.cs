#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Services;

/// <summary>
/// Database-backed distributed lock entry for a single job.
/// Ensures only one scheduler node runs a given job at a time in multi-instance deployments.
/// </summary>
public sealed class DistributedJobLock
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the ID of the locked job.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the identifier of the node that acquired the lock.</summary>
    public string HolderInstanceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the lock was acquired.</summary>
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp after which the lock expires automatically.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Returns true when the lock has passed its expiry time.</summary>
    public bool IsExpired(DateTime? utcNow = null) => (utcNow ?? DateTime.UtcNow) >= ExpiresAt;
}
