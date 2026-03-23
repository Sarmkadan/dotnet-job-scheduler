#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading;
using System.Threading.Tasks;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides distributed leader election so that only one scheduler node fires
/// jobs at each scheduled interval in a multi-instance deployment.
/// </summary>
public interface ILeaderElectionService
{
    /// <summary>Returns true when this instance currently holds the leader lease.</summary>
    bool IsLeader { get; }

    /// <summary>
    /// Attempts to acquire (or renew) the leader lease.
    /// Should be called periodically by the hosting background service.
    /// </summary>
    Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the leader lease so another node can take over immediately.
    /// Should be called on graceful shutdown.
    /// </summary>
    Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default);
}
