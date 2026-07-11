using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JobScheduler.Core.Middleware
{
    /// <summary>
    /// Extension methods that validate the public configuration of <see cref="RateLimitMiddleware"/>.
    /// The validation logic is defensive: it inspects the public members that are likely to represent
    /// configuration values (e.g., numeric limits, strings, dates) and reports any values that appear
    /// to be invalid (null, empty, zero, negative, or default dates). This approach works even if the
    /// exact set of members changes, because it relies on reflection rather than hard-coded member
    /// names.
    /// </summary>
    public static class RateLimitMiddlewareValidation
    {
        /// <summary>
        /// Returns a list of human-readable validation problems for the supplied <paramref name="value"/>.
        /// If the list is empty the instance is considered valid.
        /// </summary>
        /// <param name="value">The <see cref="RateLimitMiddleware"/> instance to validate.</param>
        /// <returns>A list of validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this RateLimitMiddleware value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Use reflection to examine public instance properties that are likely to be configuration values.
            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var prop in properties)
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var propName = prop.Name;
                var propType = prop.PropertyType;
                var propValue = prop.GetValue(value);

                // String checks
                if (propType == typeof(string))
                {
                    if (string.IsNullOrWhiteSpace((string?)propValue))
                        problems.Add($"{propName} must not be null or empty.");
                }
                // Integer checks (including int, long, short, byte, etc.)
                else if (IsIntegerType(propType))
                {
                    // Convert to long for a unified comparison
                    var numericValue = Convert.ToInt64(propValue);
                    if (numericValue <= 0)
                        problems.Add($"{propName} must be greater than zero (current value: {numericValue}).");
                }
                // TimeSpan checks
                else if (propType == typeof(TimeSpan))
                {
                    var ts = (TimeSpan?)propValue;
                    if (ts == null || ts.Value <= TimeSpan.Zero)
                        problems.Add($"{propName} must be a positive TimeSpan.");
                }
                // DateTime checks
                else if (propType == typeof(DateTime))
                {
                    var dt = (DateTime?)propValue;
                    if (dt == null || !dt.Value.IsValidDateTime())
                        problems.Add($"{propName} must be a valid (non-default) DateTime.");
                }
                // Nullable versions of the above types
                else if (Nullable.GetUnderlyingType(propType) is Type underlying)
                {
                    if (IsIntegerType(underlying))
                    {
                        if (propValue == null)
                            problems.Add($"{propName} must have a value.");
                        else
                        {
                            var numericValue = Convert.ToInt64(propValue);
                            if (numericValue <= 0)
                                problems.Add($"{propName} must be greater than zero (current value: {numericValue}).");
                        }
                    }
                    else if (underlying == typeof(TimeSpan))
                    {
                        if (propValue == null)
                            problems.Add($"{propName} must have a value.");
                        else
                        {
                            var ts = (TimeSpan)propValue;
                            if (ts <= TimeSpan.Zero)
                                problems.Add($"{propName} must be a positive TimeSpan.");
                        }
                    }
                    else if (underlying == typeof(DateTime))
                    {
                        if (propValue == null)
                            problems.Add($"{propName} must have a value.");
                        else
                        {
                            var dt = (DateTime)propValue;
                            if (!dt.IsValidDateTime())
                                problems.Add($"{propName} must be a valid (non-default) DateTime.");
                        }
                    }
                }
                // For other reference types, we only check for null if they are not collections.
                else if (!propType.IsValueType && propType != typeof(string))
                {
                    if (propValue == null)
                        problems.Add($"{propName} must not be null.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the supplied <paramref name="value"/> has no validation problems.
        /// </summary>
        /// <param name="value">The <see cref="RateLimitMiddleware"/> instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this RateLimitMiddleware value) => !value.Validate().Any();

        /// <summary>
        /// Ensures the supplied <paramref name="value"/> is valid. If not, throws an
        /// <see cref="ArgumentException"/> containing the validation problems.
        /// </summary>
        /// <param name="value">The <see cref="RateLimitMiddleware"/> instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        public static void EnsureValid(this RateLimitMiddleware value)
        {
            var problems = value.Validate();
            if (problems.Any())
            {
                var message = $"RateLimitMiddleware configuration is invalid: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }

        /// <summary>
        /// Determines whether a <see cref="DateTime"/> value is valid (not default).
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        private static bool IsValidDateTime(this DateTime dateTime) => dateTime != default;

        /// <summary>
        /// Determines whether a type is an integer type (int, long, short, byte, uint, ulong, ushort, sbyte).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is an integer type; otherwise, <see langword="false"/>.</returns>
        private static bool IsIntegerType(this Type type)
        {
            return type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(short) ||
                   type == typeof(byte) ||
                   type == typeof(uint) ||
                   type == typeof(ulong) ||
                   type == typeof(ushort) ||
                   type == typeof(sbyte);
        }
    }
}