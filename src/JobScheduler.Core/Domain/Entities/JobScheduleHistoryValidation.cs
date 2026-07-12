using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Domain.Entities
{
    /// <summary>
    /// Provides validation methods for <see cref="JobScheduleHistory"/> instances.
    /// </summary>
    public static class JobScheduleHistoryValidation
    {
        /// <summary>
        /// Validates a <see cref="JobScheduleHistory"/> instance and returns a list of validation errors.
        /// </summary>
        /// <param name="value">The job schedule history instance to validate.</param>
        /// <returns>A read-only list of validation error messages. Empty if validation succeeds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static IReadOnlyList<string> Validate(this JobScheduleHistory value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.Id == Guid.Empty)
            {
                problems.Add("Id cannot be empty.");
            }

            if (value.JobId == Guid.Empty)
            {
                problems.Add("JobId cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(value.PropertyName))
            {
                problems.Add("PropertyName cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value.ChangeReason))
            {
                problems.Add("ChangeReason cannot be null or whitespace.");
            }

            if (value.ChangedAt == default)
            {
                problems.Add("ChangedAt cannot be the default value.");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="JobScheduleHistory"/> instance is valid.
        /// </summary>
        /// <param name="value">The job schedule history instance to check.</param>
        /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static bool IsValid(this JobScheduleHistory value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return !string.IsNullOrWhiteSpace(value.PropertyName)
                   && value.JobId != Guid.Empty
                   && !string.IsNullOrWhiteSpace(value.ChangeReason);
        }

        /// <summary>
        /// Validates the specified <see cref="JobScheduleHistory"/> instance and throws an exception if validation fails.
        /// </summary>
        /// <param name="value">The job schedule history instance to validate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException">Thrown when the instance fails validation.</exception>
        public static void EnsureValid(this JobScheduleHistory value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"JobScheduleHistory validation failed: {string.Join("; ", errors)}");
            }
        }
    }
}
