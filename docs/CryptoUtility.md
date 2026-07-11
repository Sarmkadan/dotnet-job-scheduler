# CryptoUtility

The `CryptoUtility` class provides a collection of static cryptographic helper methods for common security operations in the `dotnet-job-scheduler` project. It includes secure random string generation, hashing (SHA-256, HMAC-SHA-256), AES-256 encryption and decryption, constant-time string comparison, file hashing, and password hashing with salt. All methods are designed to be used without instantiating the class.

## API

### `GenerateSecureRandomString`
Generates a cryptographically secure random string. The length and character set are determined by the implementation.  
**Returns:** A string containing random characters.  
**Throws:** `CryptographicException` if the random number generator fails.

### `GenerateTimestampedToken`
Generates a token that embeds a timestamp for expiration validation.  
**Returns:** A string representing the timestamped token.  
**Throws:** `ArgumentException` if the required parameters are invalid.

### `ComputeSha256`
Computes the SHA-256 hash of the provided data.  
**Returns:** A hexadecimal string representation of the hash.  
**Throws:** `ArgumentNullException` if the input is `null`.

### `ComputeHmacSha256`
Computes an HMAC-SHA-256 signature for the given message and key.  
**Returns:** A hexadecimal string representing the HMAC.  
**Throws:** `ArgumentNullException` if the message or key is `null`.

### `VerifyHmacSha256`
Verifies an HMAC-SHA-256 signature against a message and key.  
**Returns:** `true` if the signature matches; otherwise `false`.  
**Throws:** `ArgumentNullException` if any required parameter is `null`.

### `EncryptAes256`
Encrypts plaintext using AES-256 with a randomly generated IV.  
**Returns:** A tuple `(string Ciphertext, string Iv)` where both values are Base64-encoded strings.  
**Throws:** `ArgumentNullException` if the plaintext or key is `null`; `CryptographicException` if encryption fails.

### `DecryptAes256`
Decrypts ciphertext that was encrypted with AES-256 using the provided key and IV.  
**Returns:** The decrypted plaintext as a string.  
**Throws:** `ArgumentNullException` if any parameter is `null`; `CryptographicException` if decryption fails (e.g., wrong key or corrupted data).

### `CompareStringsSecurely`
Compares two strings in constant time to prevent timing attacks.  
**Returns:** `true` if the strings are equal; otherwise `false`.  
**Throws:** None (handles `null` inputs gracefully by returning `false`).

### `ComputeFileHash`
Computes the SHA-256 hash of a file's contents.  
**Returns:** A hexadecimal string representing the file hash.  
**Throws:** `FileNotFoundException` if the file does not exist; `IOException` if the file cannot be read.

### `GeneratePasswordHash`
Generates a salted hash of a password using a strong key derivation function.  
**Returns:** A tuple `(string Hash, string Salt)` where both values are Base64-encoded strings.  
**Throws:** `ArgumentNullException` if the password is `null`; `ArgumentException` if the password is empty.

### `VerifyPasswordHash`
Verifies a password against a previously generated salted hash.  
**Returns:** `true` if the password matches the hash; otherwise `false`.  
**Throws:** `ArgumentNullException` if any parameter is `null`; `ArgumentException` if the hash or salt format is invalid.

## Usage

### Example 1: Password Hashing and Verification

```csharp
using CryptoUtility;

// Hash a password
string password = "MySecureP@ssw0rd";
var (hash, salt) = CryptoUtility.GeneratePasswordHash(password);

// Later, verify the password
bool isValid = CryptoUtility.VerifyPasswordHash(password, hash, salt);
Console.WriteLine(isValid ? "Password correct" : "Password incorrect");
```

### Example 2: Encrypting and Decrypting a Message

```csharp
using CryptoUtility;

string plaintext = "Sensitive data";
string key = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"; // 32-byte key (Base64 or raw)

// Encrypt
var (ciphertext, iv) = CryptoUtility.EncryptAes256(plaintext, key);
Console.WriteLine($"Ciphertext: {ciphertext}");
Console.WriteLine($"IV: {iv}");

// Decrypt
string decrypted = CryptoUtility.DecryptAes256(ciphertext, key, iv);
Console.WriteLine($"Decrypted: {decrypted}");
```

## Notes

- **Thread Safety:** All methods are static and do not maintain internal state. They are thread-safe and can be called concurrently from multiple threads.
- **Null Handling:** Methods that accept string inputs throw `ArgumentNullException` when a required parameter is `null`. `CompareStringsSecurely` treats `null` inputs as non-matching and returns `false` without throwing.
- **Empty Strings:** `GeneratePasswordHash` throws `ArgumentException` for empty passwords. Other methods may accept empty strings where semantically valid (e.g., `ComputeSha256` on an empty string returns the known SHA-256 of an empty input).
- **File Hashing:** `ComputeFileHash` requires the file to exist and be readable. It does not lock the file; concurrent writes may produce inconsistent hashes.
- **Constant-Time Comparison:** `CompareStringsSecurely` uses a constant-time algorithm to mitigate timing side-channel attacks. It is safe to use for comparing secrets such as tokens or hashes.
- **Key and IV Lengths:** `EncryptAes256` and `DecryptAes256` expect a 256-bit (32-byte) key. The IV is generated internally during encryption and must be provided for decryption. Both key and IV are expected as Base64-encoded strings.
- **Randomness:** `GenerateSecureRandomString` and the internal IV generation use a cryptographically secure random number generator (`RandomNumberGenerator`). The output is suitable for security-sensitive contexts.
