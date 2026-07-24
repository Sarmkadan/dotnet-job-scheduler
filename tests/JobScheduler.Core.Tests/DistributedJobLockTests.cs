using System;
using Xunit;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Tests;

public sealed class DistributedJobLockTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeProperties()
    {
        var lockEntry = new DistributedJobLock();

        Assert.NotEqual(Guid.Empty, lockEntry.Id);
        Assert.Equal(Guid.Empty, lockEntry.JobId);
        Assert.Equal(string.Empty, lockEntry.HolderInstanceId);
        Assert.True(lockEntry.AcquiredAt <= DateTime.UtcNow);
        Assert.Equal(default(DateTime), lockEntry.ExpiresAt);
        Assert.True(lockEntry.IsExpired());
    }

    [Fact]
    public void PropertySetters_ShouldPersistValues()
    {
        var jobId = Guid.NewGuid();
        var holder = "node-1";
        var acquired = DateTime.UtcNow.AddMinutes(-5);
        var expires = DateTime.UtcNow.AddMinutes(10);

        var lockEntry = new DistributedJobLock
        {
            JobId = jobId,
            HolderInstanceId = holder,
            AcquiredAt = acquired,
            ExpiresAt = expires
        };

        Assert.Equal(jobId, lockEntry.JobId);
        Assert.Equal(holder, lockEntry.HolderInstanceId);
        Assert.Equal(acquired, lockEntry.AcquiredAt);
        Assert.Equal(expires, lockEntry.ExpiresAt);
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenUtcNowBeforeExpiresAt()
    {
        var expires = DateTime.UtcNow.AddMinutes(5);
        var lockEntry = new DistributedJobLock { ExpiresAt = expires };

        var utcNow = DateTime.UtcNow;
        Assert.False(lockEntry.IsExpired(utcNow));
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenUtcNowAfterExpiresAt()
    {
        var expires = DateTime.UtcNow.AddMinutes(-5);
        var lockEntry = new DistributedJobLock { ExpiresAt = expires };

        var utcNow = DateTime.UtcNow;
        Assert.True(lockEntry.IsExpired(utcNow));
    }

    [Fact]
    public void IsExpired_WithNullUtcNow_ShouldUseCurrentUtcNow()
    {
        var expires = DateTime.UtcNow.AddMinutes(-1);
        var lockEntry = new DistributedJobLock { ExpiresAt = expires };

        Assert.True(lockEntry.IsExpired());
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpiresAtIsDefault()
    {
        var lockEntry = new DistributedJobLock(); // ExpiresAt default
        Assert.True(lockEntry.IsExpired());
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpiresAtIsMaxValue()
    {
        var lockEntry = new DistributedJobLock { ExpiresAt = DateTime.MaxValue };
        Assert.False(lockEntry.IsExpired());
    }

    [Fact]
    public void Id_ShouldBeUniquePerInstance()
    {
        var lock1 = new DistributedJobLock();
        var lock2 = new DistributedJobLock();

        Assert.NotEqual(lock1.Id, lock2.Id);
    }
}
