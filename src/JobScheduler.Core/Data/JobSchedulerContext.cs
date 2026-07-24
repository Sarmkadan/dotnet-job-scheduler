#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Data;

/// <summary>
/// Entity Framework Core database context for the job scheduler.
/// Manages all entities, relationships, and migrations.
/// </summary>
public sealed class JobSchedulerContext : DbContext
{
    public JobSchedulerContext(DbContextOptions<JobSchedulerContext> options) : base(options) { }

    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<JobExecution> JobExecutions { get; set; } = null!;
    public DbSet<JobScheduleHistory> JobScheduleHistories { get; set; } = null!;
    public DbSet<RetryPolicy> RetryPolicies { get; set; } = null!;
    public DbSet<ExecutionMetrics> ExecutionMetrics { get; set; } = null!;
    public DbSet<JobDependency> JobDependencies { get; set; } = null!;
    public DbSet<SchedulerLeaderLock> SchedulerLeaderLocks { get; set; } = null!;
    public DbSet<JobPipeline> JobPipelines { get; set; } = null!;
    public DbSet<JobPipelineStep> JobPipelineSteps { get; set; } = null!;
    public DbSet<DistributedJobLock> DistributedJobLocks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Job entity
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.NextExecutionAt);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CronExpression).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HandlerType).HasMaxLength(512).IsRequired();
            entity.Property(e => e.MisfirePolicy).IsRequired();
        });

        // Configure JobExecution entity
        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.JobId, e.Status });
            entity.Property(e => e.ExecutorName).HasMaxLength(256).IsRequired();
        });

        // Configure JobScheduleHistory entity
        modelBuilder.Entity<JobScheduleHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.ChangedAt);
            entity.Property(e => e.PropertyName).HasMaxLength(100).IsRequired();
        });

        // Configure RetryPolicy entity
        modelBuilder.Entity<RetryPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
        });

        // Configure ExecutionMetrics entity
        modelBuilder.Entity<ExecutionMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId).IsUnique();
        });

        // Configure JobDependency entity
        modelBuilder.Entity<JobDependency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.DependsOnJobId);
            entity.HasIndex(e => new { e.JobId, e.DependsOnJobId }).IsUnique();
        });

        // Configure SchedulerLeaderLock entity
        modelBuilder.Entity<SchedulerLeaderLock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LockName).IsUnique();
            entity.Property(e => e.LockName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.LeaderInstanceId).HasMaxLength(256).IsRequired();
        });

        // Configure JobPipeline entity
        modelBuilder.Entity<JobPipeline>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
        });

        // Configure JobPipelineStep entity
        modelBuilder.Entity<JobPipelineStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PipelineId);
            entity.HasIndex(e => new { e.PipelineId, e.StepOrder }).IsUnique();
        });

        // Configure DistributedJobLock entity
        modelBuilder.Entity<DistributedJobLock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId).IsUnique();
            entity.Property(e => e.HolderInstanceId).HasMaxLength(256).IsRequired();
        });

        // Configure relationships
        modelBuilder.Entity<Job>()
            .HasMany(j => j.Executions)
            .WithOne(e => e.Job)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.ScheduleHistories)
            .WithOne(h => h.Job)
            .HasForeignKey(h => h.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobDependency>()
            .HasOne(d => d.Job)
            .WithMany()
            .HasForeignKey(d => d.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobDependency>()
            .HasOne(d => d.DependsOnJob)
            .WithMany()
            .HasForeignKey(d => d.DependsOnJobId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure JobPipeline relationships
        modelBuilder.Entity<JobPipeline>()
            .HasMany(p => p.Steps)
            .WithOne(s => s.Pipeline)
            .HasForeignKey(s => s.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobPipelineStep>()
            .HasOne(s => s.Job)
            .WithMany()
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Saves all changes to the database asynchronously.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates audit fields (CreatedAt, UpdatedAt) before saving.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is Job job)
            {
                if (entry.State == EntityState.Added)
                {
                    job.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    job.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (entry.Entity is JobScheduleHistory history && entry.State == EntityState.Added)
            {
                history.ChangedAt = DateTime.UtcNow;
            }
        }
    }
}
