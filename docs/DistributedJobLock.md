# DistributedJobLock
The `DistributedJobLock` type represents a lock acquired on a job in a distributed environment, ensuring that only one instance can execute the job at a time. This type is crucial in preventing concurrent execution of jobs across multiple instances, thereby preventing data inconsistencies and other potential issues.

## API
* `public Guid Id`: A unique identifier for the lock.
* `public Guid JobId`: The identifier of the job that this lock is acquired for.
* `public string HolderInstanceId`: The instance identifier that currently holds the lock.
* `public DateTime AcquiredAt`: The timestamp when the lock was acquired.
* `public DateTime ExpiresAt`: The timestamp when the lock is set to expire.
* `public bool IsExpired`: A flag indicating whether the lock has expired.

## Usage
The following examples demonstrate how to use the `DistributedJobLock` type:
```csharp
// Example 1: Checking if a lock has expired
DistributedJobLock lockInstance = GetLockInstanceFromDatabase();
if (lockInstance.IsExpired)
{
    // The lock has expired, a new instance can acquire the lock
    AcquireNewLock(lockInstance.JobId);
}
```

```csharp
// Example 2: Verifying the lock holder
DistributedJobLock lockInstance = GetLockInstanceFromDatabase();
if (lockInstance.HolderInstanceId == GetCurrentInstanceId())
{
    // The current instance is the lock holder, it can execute the job
    ExecuteJob(lockInstance.JobId);
}
```

## Notes
When working with `DistributedJobLock`, consider the following edge cases and thread-safety remarks:
* Lock expiration is based on the `ExpiresAt` timestamp, which should be set considering the maximum execution time of the job and potential network delays.
* The `IsExpired` flag is a snapshot of the lock's state at a given time and may not reflect the current state if the lock has been updated concurrently.
* Access to `DistributedJobLock` instances should be thread-safe, as multiple threads may attempt to acquire or release locks simultaneously.
* In a distributed environment, clock skew between instances can affect lock expiration and acquisition. It is essential to ensure that all instances have synchronized clocks to prevent inconsistencies.
