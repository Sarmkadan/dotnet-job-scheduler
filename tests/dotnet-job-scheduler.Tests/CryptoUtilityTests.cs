using System;
using JobScheduler.Core.Utilities;
using Xunit;

namespace dotnet_job_scheduler.Tests
{
    public class CryptoUtilityTests
    {
        [Fact]
        public void ComputeSha256_Deterministic_ReturnsSameHashForSameInput()
        {
            // Arrange
            var input = "deterministic-test";

            // Act
            var hash1 = CryptoUtility.ComputeSha256(input);
            var hash2 = CryptoUtility.ComputeSha256(input);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ComputeSha256_DifferentInputs_ReturnDifferentHashes()
        {
            // Arrange
            var input1 = "first-input";
            var input2 = "second-input";

            // Act
            var hash1 = CryptoUtility.ComputeSha256(input1);
            var hash2 = CryptoUtility.ComputeSha256(input2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ComputeSha256_EmptyInput_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.ComputeSha256(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ComputeSha256_NullInput_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.ComputeSha256(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EncryptAes256_And_DecryptAes256_RoundTrip_ReturnsOriginalPlaintext()
        {
            // Arrange
            var key = "super-secret-key-12345";
            var plaintext = "The quick brown fox jumps over the lazy dog";

            // Act
            var (ciphertext, iv) = CryptoUtility.EncryptAes256(plaintext, key);
            var decrypted = CryptoUtility.DecryptAes256(ciphertext, key, iv);

            // Assert
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void EncryptAes256_EmptyInput_ReturnsEmptyCipherAndIv()
        {
            // Act
            var (ciphertext, iv) = CryptoUtility.EncryptAes256(string.Empty, "any-key");

            // Assert
            Assert.Equal(string.Empty, ciphertext);
            Assert.Equal(string.Empty, iv);
        }

        [Fact]
        public void EncryptAes256_NullInput_ReturnsEmptyCipherAndIv()
        {
            // Act
            var (ciphertext, iv) = CryptoUtility.EncryptAes256(null, "any-key");

            // Assert
            Assert.Equal(string.Empty, ciphertext);
            Assert.Equal(string.Empty, iv);
        }

        [Fact]
        public void DecryptAes256_EmptyCipher_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.DecryptAes256(string.Empty, "key", "iv");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DecryptAes256_NullCipher_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.DecryptAes256(null, "key", "iv");

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
