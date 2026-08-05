#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for the generic <see cref="Repository{T}"/> implementation.
/// Uses a Sqlite in-memory connection so the repository exercises a real
/// EF Core provider instead of a mocked DbSet.
/// </summary>
public sealed class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly JobSchedulerContext _context;
    private readonly Repository<Job> _repository;

    public RepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new JobSchedulerContext(options);
        _context.Database.EnsureCreated();

        _repository = new Repository<Job>(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Job CreateJob(string name = "test-job") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CronExpression = "* * * * *",
        HandlerType = "SampleHandler"
    };

    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Repository<Job>(null!));
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameEntity()
    {
        var job = CreateJob();

        await _repository.AddAsync(job);
        await _repository.SaveChangesAsync();

        var fetched = await _repository.GetByIdAsync(job.Id);

        Assert.NotNull(fetched);
        Assert.Equal(job.Name, fetched!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_AddsNothingAndCountRemainsZero()
    {
        await _repository.AddRangeAsync(Enumerable.Empty<Job>());
        await _repository.SaveChangesAsync();

        var count = await _repository.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAddedEntities()
    {
        await _repository.AddRangeAsync(new[] { CreateJob("job-1"), CreateJob("job-2") });
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task FindAsync_And_FirstOrDefaultAsync_FilterByPredicate()
    {
        var target = CreateJob("target-job");
        await _repository.AddRangeAsync(new[] { CreateJob("other-job"), target });
        await _repository.SaveChangesAsync();

        var found = await _repository.FindAsync(j => j.Name == "target-job");
        var first = await _repository.FirstOrDefaultAsync(j => j.Name == "target-job");
        var missing = await _repository.FirstOrDefaultAsync(j => j.Name == "does-not-exist");

        Assert.Single(found);
        Assert.NotNull(first);
        Assert.Equal(target.Id, first!.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CountAsync_WithAndWithoutPredicate_ReturnsExpectedCounts()
    {
        await _repository.AddRangeAsync(new[] { CreateJob("job-a"), CreateJob("job-b") });
        await _repository.SaveChangesAsync();

        var total = await _repository.CountAsync();
        var filtered = await _repository.CountAsync(j => j.Name == "job-a");

        Assert.Equal(2, total);
        Assert.Equal(1, filtered);
    }

    [Fact]
    public void Update_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repository.Update(null!));
    }

    [Fact]
    public async Task Update_ExistingEntity_PersistsChanges()
    {
        var job = CreateJob();
        await _repository.AddAsync(job);
        await _repository.SaveChangesAsync();

        job.Name = "renamed-job";
        _repository.Update(job);
        await _repository.SaveChangesAsync();

        var reloaded = await _repository.GetByIdAsync(job.Id);

        Assert.Equal("renamed-job", reloaded!.Name);
    }

    [Fact]
    public void UpdateRange_NullEntities_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repository.UpdateRange(null!));
    }

    [Fact]
    public async Task UpdateRange_ExistingEntities_PersistsAllChanges()
    {
        var jobs = new List<Job> { CreateJob("job-x"), CreateJob("job-y") };
        await _repository.AddRangeAsync(jobs);
        await _repository.SaveChangesAsync();

        foreach (var job in jobs)
        {
            job.Status = JobStatus.Suspended;
        }

        _repository.UpdateRange(jobs);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        Assert.All(all, j => Assert.Equal(JobStatus.Suspended, j.Status));
    }
}
