# DatabaseLeaderElectionService

A service that provides leader election capabilities using a database-backed distributed lock. It ensures only one instance in a cluster can act as the leader at any given time, coordinating through a shared database record identified by a lock name. The service tracks leadership acquisition time, lease expiration, and the current leader's instance identifier.

## API

### `DatabaseLeaderElectionService`

Initializes a new instance of the leader election service.

No parameters are required for construction; dependencies such as a database connection and instance identifier are typically injected via constructor parameters (not shown in public members).

### `async Task<bool> TryAcquireLeadershipAsync()`

Attempts to acquire leadership for the current instance. Returns `true` if leadership was successfully acquired; otherwise `false`.

This method is idempotent for the same instance—subsequent calls by the same instance may return `true` if leadership is still held.

No parameters.

Throws:
- `InvalidOperationException` if the service is disposed.
- `DbUpdateException` or other database-related exceptions if the underlying operation fails.

### `async Task ReleaseLeadershipAsync()`

Releases leadership held by the current instance. If the instance does not currently hold leadership, this method has no effect.

No parameters.

Throws:
- `InvalidOperationException` if the service is disposed.

### `int Id`

Gets the unique identifier of the leader election service instance.

This value is set at construction and does not change.

### `string LockName`

Gets the name of the distributed lock used for leader election.

This value is set at construction and identifies the shared resource for which leadership is contested.

### `string LeaderInstanceId`

Gets the instance identifier of the current leader, or `null` if no leader is elected.

This value is updated when leadership is acquired or released.

### `DateTime LeaseExpiresAt`

Gets the timestamp when the current leadership lease expires.

This value is updated when leadership is acquired and reflects the expiration time of the lock.

### `DateTime AcquiredAt`

Gets the timestamp when leadership was most recently acquired by the current instance, or `default` if leadership has never been acquired.

This value is updated when leadership is acquired.

## Usage

### Example: Acquire and maintain leadership in a background service
