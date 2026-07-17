#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides validation helpers for leader election entities managed by <see cref="DatabaseLeaderElectionService"/>.
/// </summary>
/// <remarks>
/// This static class contains extension methods for <see cref="SchedulerLeaderLock"/> that validate the entity's state
/// according to business rules for distributed leader election. All validation methods ensure data integrity
/// before it is persisted to the database.
/// </remarks>
public static class DatabaseLeaderElectionServiceValidation
{
	/// <summary>
	/// Validates the <see cref="SchedulerLeaderLock"/> entity used by <see cref="DatabaseLeaderElectionService"/>.
	/// </summary>
	/// <param name="value">The leader lock entity to validate.</param>
	/// <returns>A list of human-readable validation problems; empty when valid.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static IReadOnlyList<string> Validate(this SchedulerLeaderLock? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = new List<string>();

		// Validate Id
		if (value.Id <= 0)
		{
			problems.Add($"Id must be a positive integer, but was {value.Id}.");
		}

		// Validate LockName
		if (string.IsNullOrWhiteSpace(value.LockName))
		{
			problems.Add("LockName cannot be null or whitespace.");
		}
		else if (value.LockName.Length > 100)
		{
			problems.Add("LockName cannot exceed 100 characters.");
		}

		// Validate LeaderInstanceId
		if (string.IsNullOrWhiteSpace(value.LeaderInstanceId))
		{
			problems.Add("LeaderInstanceId cannot be null or whitespace.");
		}
		else if (value.LeaderInstanceId.Length > 100)
		{
			problems.Add("LeaderInstanceId cannot exceed 100 characters.");
		}

		// Validate LeaseExpiresAt
		if (value.LeaseExpiresAt == default)
		{
			problems.Add("LeaseExpiresAt cannot be the default DateTime value.");
		}
		else if (value.LeaseExpiresAt.Kind != DateTimeKind.Utc)
		{
			problems.Add("LeaseExpiresAt must be in UTC format.");
		}
		else if (value.LeaseExpiresAt < DateTime.UtcNow.AddMinutes(-5))
		{
			problems.Add("LeaseExpiresAt cannot be in the past (more than 5 minutes ago).");
		}

		// Validate AcquiredAt
		if (value.AcquiredAt == default)
		{
			problems.Add("AcquiredAt cannot be the default DateTime value.");
		}
		else if (value.AcquiredAt.Kind != DateTimeKind.Utc)
		{
			problems.Add("AcquiredAt must be in UTC format.");
		}
		else if (value.AcquiredAt > DateTime.UtcNow.AddMinutes(5))
		{
			problems.Add("AcquiredAt cannot be in the future (more than 5 minutes ahead).");
		}

		// Validate that AcquiredAt is not after LeaseExpiresAt
		if (value.LeaseExpiresAt != default && value.AcquiredAt != default && value.AcquiredAt > value.LeaseExpiresAt)
		{
			problems.Add("AcquiredAt cannot be after LeaseExpiresAt.");
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Determines whether the specified <see cref="SchedulerLeaderLock"/> entity is valid.
	/// </summary>
	/// <param name="value">The leader lock entity to check.</param>
	/// <returns>True if the entity is valid; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static bool IsValid(this SchedulerLeaderLock? value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Validate(value).Count == 0;
	}

	/// <summary>
	/// Ensures that the specified <see cref="SchedulerLeaderLock"/> entity is valid.
	/// </summary>
	/// <param name="value">The leader lock entity to validate.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the entity is not valid, containing a list of problems.</exception>
	public static void EnsureValid(this SchedulerLeaderLock? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = Validate(value);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				$"SchedulerLeaderLock is not valid. Problems: {string.Join(" ", problems)}");
		}
	}
}