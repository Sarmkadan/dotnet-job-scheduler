using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Tests
{
    public class AuditLoggerJsonExtensionsTests
    {
        [Fact]
        public void ToJson_AuditLogEntry_ReturnsJsonString()
        {
            // Arrange
            var auditLogEntry = new AuditLogEntry();

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(auditLogEntry);

            // Assert
            Assert.NotNull(actualJson);
            Assert.StartsWith("{", actualJson);
            Assert.EndsWith("}", actualJson);
            // Verify it round-trips
            var deserialized = JsonSerializer.Deserialize<AuditLogEntry>(actualJson);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void ToJson_ApiCallAudit_ReturnsJsonString()
        {
            // Arrange
            var apiCallAudit = new ApiCallAudit();

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(apiCallAudit);

            // Assert
            Assert.NotNull(actualJson);
            Assert.StartsWith("{", actualJson);
            Assert.EndsWith("}", actualJson);
            var deserialized = JsonSerializer.Deserialize<ApiCallAudit>(actualJson);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void ToJson_AuditStatistics_ReturnsJsonString()
        {
            // Arrange
            var auditStatistics = new AuditStatistics();

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(auditStatistics);

            // Assert
            Assert.NotNull(actualJson);
            Assert.StartsWith("{", actualJson);
            Assert.EndsWith("}", actualJson);
            var deserialized = JsonSerializer.Deserialize<AuditStatistics>(actualJson);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void FromJsonToAuditLogEntry_NullInput_ReturnsNull()
        {
            // Act
            var actualAuditLogEntry = AuditLoggerJsonExtensions.FromJsonToAuditLogEntry(null);

            // Assert
            Assert.Null(actualAuditLogEntry);
        }

        [Fact]
        public void FromJsonToAuditLogEntry_EmptyJson_ReturnsNull()
        {
            // Act
            var actualAuditLogEntry = AuditLoggerJsonExtensions.FromJsonToAuditLogEntry("");

            // Assert
            Assert.Null(actualAuditLogEntry);
        }

        [Fact]
        public void TryFromJsonToAuditLogEntry_NullInput_ReturnsFalse()
        {
            // Act
            var actualResult = AuditLoggerJsonExtensions.TryFromJsonToAuditLogEntry(null, out _);

            // Assert
            Assert.False(actualResult);
        }

        [Fact]
        public void TryFromJsonToAuditLogEntry_EmptyJson_ReturnsFalse()
        {
            // Act
            var actualResult = AuditLoggerJsonExtensions.TryFromJsonToAuditLogEntry("", out _);

            // Assert
            Assert.False(actualResult);
        }
    }
}
