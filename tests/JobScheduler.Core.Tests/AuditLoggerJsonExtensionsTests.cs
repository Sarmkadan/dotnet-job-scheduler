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
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(auditLogEntry);

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_ApiCallAudit_ReturnsJsonString()
        {
            // Arrange
            var apiCallAudit = new ApiCallAudit();
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(apiCallAudit);

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_AuditStatistics_ReturnsJsonString()
        {
            // Arrange
            var auditStatistics = new AuditStatistics();
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = AuditLoggerJsonExtensions.ToJson(auditStatistics);

            // Assert
            Assert.Equal(expectedJson, actualJson);
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
