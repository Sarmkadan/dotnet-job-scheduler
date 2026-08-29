using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace JobScheduler.Core.Tests
{
    public class TimeUtilityTests
    {
        [Fact]
        public void FromUnixTimestamp_EpochZero_ReturnsUnixEpoch()
        {
            var result = Utilities.TimeUtility.FromUnixTimestamp(0);

            Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void FromUnixTimestamp_KnownTimestamp_ReturnsExpectedUtcDateTime()
        {
            var result = Utilities.TimeUtility.FromUnixTimestamp(1_704_067_200);

            Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void ToUnixTimestamp_EpochZero_ReturnsZero()
        {
            var result = Utilities.TimeUtility.ToUnixTimestamp(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(0, result);
        }

        [Fact]
        public void UnixTimestamp_RoundTrip_PreservesWholeSecondDateTime()
        {
            var expected = new DateTime(2032, 6, 15, 10, 30, 45, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.FromUnixTimestamp(
                Utilities.TimeUtility.ToUnixTimestamp(expected));

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToIso8601_UtcDateTime_ReturnsRoundTripFormat()
        {
            var dateTime = new DateTime(2025, 3, 8, 14, 5, 9, 123, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.ToIso8601(dateTime);

            Assert.Equal("2025-03-08T14:05:09.1230000Z", result);
        }

        [Fact]
        public void ParseIso8601_FormattedDateTime_RoundTripPreservesUtcValue()
        {
            var expected = new DateTime(2025, 10, 12, 7, 8, 9, 456, DateTimeKind.Utc);
            var formatted = Utilities.TimeUtility.ToIso8601(expected);

            var result = Utilities.TimeUtility.ParseIso8601(formatted);

            Assert.Equal(expected, result);
            Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        }

        [Fact]
        public void ParseIso8601_NullInput_ReturnsNull()
        {
            var result = Utilities.TimeUtility.ParseIso8601(null);

            Assert.Null(result);
        }

        [Fact]
        public void ParseIso8601_GarbageInput_ReturnsNull()
        {
            var result = Utilities.TimeUtility.ParseIso8601("not-a-date");

            Assert.Null(result);
        }

        [Fact]
        public void RoundDown_ExactBoundary_ReturnsSameDateTime()
        {
            var expected = new DateTime(2025, 4, 2, 14, 30, 0, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.RoundDown(expected, TimeSpan.FromMinutes(15));

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RoundDown_MidInterval_ReturnsPreviousBoundary()
        {
            var dateTime = new DateTime(2025, 4, 2, 14, 37, 0, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.RoundDown(dateTime, TimeSpan.FromMinutes(15));

            Assert.Equal(new DateTime(2025, 4, 2, 14, 30, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void RoundUp_ExactBoundary_ReturnsSameDateTime()
        {
            var expected = new DateTime(2025, 4, 2, 14, 30, 0, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.RoundUp(expected, TimeSpan.FromMinutes(15));

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RoundUp_MidInterval_ReturnsNextBoundary()
        {
            var dateTime = new DateTime(2025, 4, 2, 14, 37, 0, DateTimeKind.Utc);

            var result = Utilities.TimeUtility.RoundUp(dateTime, TimeSpan.FromMinutes(15));

            Assert.Equal(new DateTime(2025, 4, 2, 14, 45, 0, DateTimeKind.Utc), result);
        }

        [Fact]
        public void GetAge_BirthdayReachedThisYear_ReturnsCurrentAge()
        {
            var birthDate = new DateTime(1990, 3, 10);
            var referenceDate = new DateTime(2025, 3, 10);

            var result = Utilities.TimeUtility.GetAge(birthDate, referenceDate);

            Assert.Equal(35, result);
        }

        [Fact]
        public void GetAge_BirthdayNotYetReachedThisYear_SubtractsOneYear()
        {
            var birthDate = new DateTime(1990, 10, 20);
            var referenceDate = new DateTime(2025, 8, 15);

            var result = Utilities.TimeUtility.GetAge(birthDate, referenceDate);

            Assert.Equal(34, result);
        }

        [Fact]
        public void IsBetweenTimes_NormalRangeAtBoundary_ReturnsTrue()
        {
            var time = new DateTime(2025, 1, 1, 9, 0, 0);

            var result = Utilities.TimeUtility.IsBetweenTimes(
                time, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

            Assert.True(result);
        }

        [Fact]
        public void IsBetweenTimes_RangeCrossingMidnight_ReturnsFalseWithDirectComparison()
        {
            var time = new DateTime(2025, 1, 2, 1, 0, 0);

            var result = Utilities.TimeUtility.IsBetweenTimes(
                time, TimeSpan.FromHours(22), TimeSpan.FromHours(2));

            Assert.False(result);
        }
    }
}
