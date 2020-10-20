# DistributedJobLockService

The `DistributedJobLockService` provides a mechanism for coordinating access to shared resources across multiple instances of the `dotnet-job-scheduler` application. It manages the lifecycle of distributed locks, allowing jobs to safely acquire, hold, renew, and release exclusive rights to execute specific tasks without collision in a multi-node environment. This service ensures that only one job instance can hold a lock for a given identifier at any time, preventing duplicate executions and maintaining data consistency.

## API

### Constructor
**`public DistributedJobLockService()`**
Initializes a new instance of the `DistributedJobLockService`. This constructor sets up the necessary internal state and connections required to manage distributed locking operations.

### TryAcquireLockAsync
**`public async Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan leaseTime, CancellationToken cancellationToken = default)`**
Attempts to acquire an exclusive lock for the specified key.
*   **Parameters**:
    *   `lockKey`: The unique identifier for the resource to be locked.
    *   `leaseTime`: The duration for which the lock should be held before automatically expiring.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: `true` if the lock was successfully acquired; `false` if the lock is already held by another instance.
*   **Throws**: Throws `OperationCanceledException` if the `cancellationToken` is triggered. May throw network-related exceptions if the underlying storage provider is unreachable.

### ReleaseLockAsync
**`public async Task ReleaseLockAsync(string lockKey, string lockId, CancellationToken cancellationToken = default)`**
Explicitly releases a held lock, making the resource available to other instances immediately.
*   **Parameters**:
    *   `lockKey`: The identifier of the locked resource.
    *   `lockId`: The unique identifier of the specific lock instance to release (ensures ownership validation).
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: A `Task` representing the asynchronous operation.
*   **Throws**: Throws `InvalidOperationException` if the provided `lockId` does not match the current owner of the lock. Throws `OperationCanceledException` if cancellation is requested.

### IsLockedAsync
**`public async Task<bool> IsLockedAsync(string lockKey, CancellationToken cancellationToken = default)`**
Checks the current status of a lock without attempting to acquire it.
*   **Parameters**:
    *   `lockKey`: The identifier of the resource to check.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: `true` if the resource is currently locked and not expired; `false` otherwise.
*   **Throws**: Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### RenewLockAsync
**`public async Task<bool> RenewLockAsync(string lockKey, string lockId, TimeSpan additionalLeaseTime, CancellationToken cancellationToken = default)`**
Extends the expiration time of an existing lock to prevent it from expiring while a long-running job is still processing.
*   **Parameters**:
    *   `lockKey`: The identifier of the locked resource.
    *   `lockId`: The unique identifier of the lock to renew.
    *   `additionalLeaseTime`: The amount of time to add to the current expiration.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: `true` if the lock was successfully renewed; `false` if the lock no longer exists or the `lockId` does not match the owner.
*   **Throws**: Throws `OperationCanceledException` if cancellation is requested.

### GetActiveLocksAsync
**`public async Task<IReadOnlyList<DistributedJobLock>> GetActiveLocksAsync(CancellationToken cancellationToken = default)`**
Retrieves a snapshot of all currently active, non-expired locks in the system.
*   **Parameters**:
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: A read-only list of `DistributedJobLock` objects containing details about each active lock.
*   **Throws**: Throws `OperationCanceledException` if the `cancellationToken` is triggered.

### CleanExpiredLocksAsync
**`public async Task<int> CleanExpiredLocksAsync(CancellationToken cancellationToken = default)`**
Scans the lock storage and removes entries that have passed their expiration time.
*   **Parameters**:
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: The number of expired lock entries that were successfully removed.
*   **Throws**: Throws `OperationCanceledException` if cancellation is requested.

## Usage

### Example 1: Standard Lock Acquisition and Release
This example demonstrates acquiring a lock for a specific job, executing the critical section, and ensuring the lock is released even if an exception occurs.

```csharp
public async Task ExecuteJobAsync(DistributedJobLockService lockService, string jobId)
{
    var lockKey = $"job:{jobId}";
    var leaseTime = TimeSpan.FromMinutes(5);
    string? acquiredLockId = null;

    try
    {
        if (await lockService.TryAcquireLockAsync(lockKey, leaseTime))
        {
            // In a real implementation, TryAcquireLockAsync would likely return the lockId 
            // or store it in a context. For this example, assume we retrieve it via a helper 
            // or the method signature implies we track the successful acquisition.
            // Note: Actual lockId retrieval depends on specific implementation details of the return.
            // Assuming a pattern where we fetch the active lock to get the ID for release.
            var activeLocks = await lockService.GetActiveLocksAsync();
            var myLock = activeLocks.FirstOrDefault(l => l.Key == lockKey);
            
            if (myLock != null)
            {
                acquiredLockId = myLock.Id;
                
                // Simulate job execution
                await PerformCriticalOperationAsync();
            }
        }
        else
        {
            Console.WriteLine($"Job {jobId} is already running on another instance.");
        }
    }
    finally
    {
        if (acquiredLockId != null)
        {
            await lockService.ReleaseLockAsync(lockKey, acquiredLockId);
        }
    }
}
```

### Example 2: Long-Running Job with Lock Renewal
For jobs that may exceed the initial lease time, this pattern shows how to periodically renew the lock to prevent premature expiration.

```csharp
public async Task ExecuteLongRunningJobAsync(DistributedJobLockService lockService, string jobId)
{
    var lockKey = $"job:{jobId}";
    var leaseTime = TimeSpan.FromSeconds(30);
    
    if (!await lockService.TryAcquireLockAsync(lockKey, leaseTime))
    {
        return; // Cannot acquire lock
    }

    // Retrieve the lock ID immediately after acquisition (implementation dependent)
    var activeLocks = await lockService.GetActiveLocksAsync();
    var currentLock = activeLocks.First(l => l.Key == lockKey);
    var lockId = currentLock.Id;

    using var cts = new CancellationTokenSource();
    
    // Start a background task to renew the lock
    var renewalTask = Task.Run(async () =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(leaseTime / 2, cts.Token);
            bool renewed = await lockService.RenewLockAsync(lockKey, lockId, leaseTime, cts.Token);
            
            if (!renewed)
            {
                // Lock was lost or expired unexpectedly
                cts.Cancel();
                break;
            }
        }
    });

    try
    {
        // Simulate long running process
        await Task.Delay(TimeSpan.FromMinutes(2));
    }
    finally
    {
        cts.Cancel();
        await renewalTask;
        await lockService.ReleaseLockAsync(lockKey, lockId);
    }
}
```

## Notes

*   **Ownership Validation**: The `ReleaseLockAsync` and `RenewLockAsync` methods require the specific `lockId` associated with the acquisition. This prevents one instance from accidentally releasing or renewing a lock held by a different instance that acquired the same key after the first lock expired.
*   **Clock Skew**: Since distributed systems may have slight clock differences, the `CleanExpiredLocksAsync` method should be run periodically by a maintenance task or a dedicated node to ensure stale locks do not persist indefinitely.
*   **Thread Safety**: The methods on `DistributedJobLockService` are designed to be thread-safe for concurrent calls within the same application instance. However, logical race conditions can still occur between checking `IsLockedAsync` and calling `TryAcquireLockAsync`; therefore, `TryAcquireLockAsync` should always be used as the atomic entry point for acquiring locks.
*   **Expiration Handling**: If a job crashes without calling `ReleaseLockAsync`, the lock will remain until its `leaseTime` expires. Until that time, or until `CleanExpiredLocksAsync` is executed, other instances will be unable to acquire the lock.
*   **Return Values**: A `false` return from `RenewLockAsync` indicates the lock is no longer valid (either expired or stolen by another node after expiration). The calling process should stop execution of the protected task immediately to avoid data corruption.
