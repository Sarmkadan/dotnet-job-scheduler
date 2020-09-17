using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Domain.Entities
{
    public static class JobScheduleHistoryValidation
    {
        public static IReadOnlyList<string> Validate(this JobScheduleHistory value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("JobScheduleHistory instance cannot be null.");
                return problems;
            }

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

            return problems;
        }

        public static bool IsValid(this JobScheduleHistory value)
        {
            return value.Validate().Count == 0;
        }

        public static void EnsureValid(this JobScheduleHistory value)
        {
            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"JobScheduleHistory validation failed: {string.Join("; ", errors)}");
            }
        }
    }
}
