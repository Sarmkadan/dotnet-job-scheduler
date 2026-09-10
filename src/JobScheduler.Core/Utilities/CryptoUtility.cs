#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Cryptographic utility for secure operations like hashing and encryption.
/// Used for securing sensitive job parameters and generating unique identifiers.
/// WHY: Centralizing crypto prevents insecure implementations scattered across codebase.
/// </summary>
public static class CryptoUtility
{
    /// <summary>
    /// Default length for generated secure random strings.
    /// </summary>
    private const int DefaultSecureRandomStringLength = 32;

    /// <summary>
    /// Size of nonce/random bytes in timestamped tokens.
    /// </summary>
    private const int TimestampedTokenNonceSize = 12;

    /// <summary>
    /// Key size for AES encryption in bits.
    /// </summary>
    private const int AesKeySizeBits = 256;

    /// <summary>
    /// Salt size for PBKDF2 key derivation in bytes.
    /// </summary>
    private const int Pbkdf2SaltSizeBytes = 16;

    /// <summary>
    /// Iteration count for PBKDF2 key derivation.
    /// </summary>
    private const int Pbkdf2IterationCount = 10000;

    /// <summary>
    /// Length of derived keys in bytes.
    /// </summary>
    private const int DerivedKeySizeBytes = 32;
    /// <summary>
    /// Generates a cryptographically secure random string.
    /// Used for job tokens and temporary identifiers.
    /// </summary>
    public static string GenerateSecureRandomString(int length = DefaultSecureRandomStringLength)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero", nameof(length));

        using (var rng = RandomNumberGenerator.Create())
        {
            var tokenData = new byte[length];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }

    /// <summary>
    /// Generates a GUID-based token with timestamp embedded.
    /// Useful for single-use execution tokens with expiration.
    /// </summary>
    public static string GenerateTimestampedToken()
    {
        var timestamp = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var random = new byte[TimestampedTokenNonceSize];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }

        var combined = timestamp.Concat(random).ToArray();
        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Computes SHA-256 hash of input string.
    /// WHY: SHA256 is collision-resistant and widely supported for data integrity.
    /// </summary>
    public static string ComputeSha256(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Computes HMAC-SHA256 for message authentication.
    /// Used for webhook signature verification.
    /// </summary>
    public static string ComputeHmacSha256(string message, string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Verifies HMAC-SHA256 signature.
    /// Returns true if signature is valid and matches expected value.
    /// </summary>
    public static bool VerifyHmacSha256(string message, string signature, string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(signature);
        ArgumentException.ThrowIfNullOrEmpty(secret);

        try
        {
            var expected = ComputeHmacSha256(message, secret);
            return CompareStringsSecurely(expected, signature);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Encrypts string using AES-256-CBC.
    /// WHY: AES256 is NIST-approved and provides strong encryption for sensitive data.
    /// </summary>
    public static (string Ciphertext, string Iv) EncryptAes256(string plaintext, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        ArgumentException.ThrowIfNullOrEmpty(key);

        using (var aes = Aes.Create())
        {
            aes.KeySize = AesKeySizeBits;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Derive key from input
            using (var kdf = new Rfc2898DeriveBytes(key, Pbkdf2SaltSizeBytes, Pbkdf2IterationCount, HashAlgorithmName.SHA256))
            {
                aes.Key = kdf.GetBytes(DerivedKeySizeBytes);
            }

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                // Write IV to stream (needed for decryption)
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plaintext);
                }

                var ciphertext = Convert.ToBase64String(ms.ToArray());
                var iv = Convert.ToBase64String(aes.IV);

                return (ciphertext, iv);
            }
        }
    }

    /// <summary>
    /// Decrypts AES-256-CBC ciphertext.
    /// </summary>
    public static string DecryptAes256(string ciphertext, string key, string iv)
    {
        ArgumentException.ThrowIfNullOrEmpty(ciphertext);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(iv);

        try
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = AesKeySizeBits;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Derive key from input
                using (var kdf = new Rfc2898DeriveBytes(key, Pbkdf2SaltSizeBytes, Pbkdf2IterationCount, HashAlgorithmName.SHA256))
                {
                    aes.Key = kdf.GetBytes(DerivedKeySizeBytes);
                }

                aes.IV = Convert.FromBase64String(iv);

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(Convert.FromBase64String(ciphertext)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Performs constant-time string comparison.
    /// Prevents timing attacks when comparing sensitive values like HMAC signatures.
    /// WHY: Regular string comparison can leak information through execution time.
    /// </summary>
    public static bool CompareStringsSecurely(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Generates cryptographic hash for file integrity verification.
    /// Compares two files by their SHA256 hashes.
    /// </summary>
    public static string ComputeFileHash(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Generates a password hash using PBKDF2.
    /// Used for securely storing API keys and tokens.
    /// </summary>
    public static (string Hash, string Salt) GeneratePasswordHash(string password, int iterations = Pbkdf2IterationCount)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        using (var rng = RandomNumberGenerator.Create())
        {
            var salt = new byte[Pbkdf2SaltSizeBytes];
            rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(DerivedKeySizeBytes);
                return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
            }
        }
    }

    /// <summary>
    /// Verifies password against stored hash.
    /// </summary>
    public static bool VerifyPasswordHash(string password, string hash, string salt, int iterations = Pbkdf2IterationCount)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(hash);
        ArgumentException.ThrowIfNullOrEmpty(salt);

        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
            {
                var hashBytes = pbkdf2.GetBytes(DerivedKeySizeBytes);
                var hashString = Convert.ToBase64String(hashBytes);
                return CompareStringsSecurely(hashString, hash);
            }
        }
        catch
        {
            return false;
        }
    }
}
