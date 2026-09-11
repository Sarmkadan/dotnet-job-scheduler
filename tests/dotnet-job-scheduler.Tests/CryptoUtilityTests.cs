using System;
using JobScheduler.Core.Utilities;
using Xunit;

namespace dotnet_job_scheduler.Tests
{
    /// <summary>
    /// Tests for the <see cref="CryptoUtility"/> class.
    /// </summary>
    public class CryptoUtilityTests
    {
        /// <summary>
        /// Verifies that computing the SHA-256 hash of the same input is deterministic and returns the same hash.
        /// </summary>
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

        /// <summary>
        /// Verifies that computing the SHA-256 hash of different inputs returns different hashes.
        /// </summary>
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

        /// <summary>
        /// Verifies that computing the SHA-256 hash of an empty input returns an empty string.
        /// </summary>
        [Fact]
        public void ComputeSha256_EmptyInput_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.ComputeSha256(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies that computing the SHA-256 hash of a null input returns an empty string.
        /// </summary>
        [Fact]
        public void ComputeSha256_NullInput_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.ComputeSha256(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies that an AES-256 encrypt/decrypt round trip returns the original plaintext.
        /// </summary>
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

        /// <summary>
        /// Verifies that encrypting an empty input with AES-256 returns empty ciphertext and IV.
        /// </summary>
        [Fact]
        public void EncryptAes256_EmptyInput_ReturnsEmptyCipherAndIv()
        {
            // Act
            var (ciphertext, iv) = CryptoUtility.EncryptAes256(string.Empty, "any-key");

            // Assert
            Assert.Equal(string.Empty, ciphertext);
            Assert.Equal(string.Empty, iv);
        }

        /// <summary>
        /// Verifies that encrypting a null input with AES-256 returns empty ciphertext and IV.
        /// </summary>
        [Fact]
        public void EncryptAes256_NullInput_ReturnsEmptyCipherAndIv()
        {
            // Act
            var (ciphertext, iv) = CryptoUtility.EncryptAes256(null, "any-key");

            // Assert
            Assert.Equal(string.Empty, ciphertext);
            Assert.Equal(string.Empty, iv);
        }

        /// <summary>
        /// Verifies that decrypting an empty ciphertext with AES-256 returns an empty string.
        /// </summary>
        [Fact]
        public void DecryptAes256_EmptyCipher_ReturnsEmptyString()
        {
            // Act
            var result = CryptoUtility.DecryptAes256(string.Empty, "key", "iv");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies that decrypting a null ciphertext with AES-256 returns an empty string.
        /// </summary>
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
