<!-- ... existing content ... -->

## CryptoUtility

The `CryptoUtility` class provides a collection of cryptographic utility methods for secure operations such as generating random strings, computing hashes, encryption, and more. It's used to secure sensitive data like job tokens, API keys, and job parameters.

### Usage

```csharp
using JobScheduler.Core.Utilities;

// Generate a cryptographically secure random string (e.g., for tokens)
var secureRandomString = CryptoUtility.GenerateSecureRandomString(32);
Console.WriteLine($"Secure random string: {secureRandomString}");

// Generate a timestamped token (useful for single-use execution tokens)
var timestampedToken = CryptoUtility.GenerateTimestampedToken();
Console.WriteLine($"Timestamped token: {timestampedToken}");

// Compute SHA-256 hash of a string (for data integrity)
var input = "Hello, World!";
var sha256Hash = CryptoUtility.ComputeSha256(input);
Console.WriteLine($"SHA256 hash of '{input}': {sha256Hash}");

// Compute HMAC-SHA256 for message authentication (webhook signatures, etc.)
var message = "This is a test message";
var secretKey = "your_secret_key_here";
var hmacSha256 = CryptoUtility.ComputeHmacSha256(message, secretKey);
Console.WriteLine($"HMAC-SHA256 of '{message}': {hmacSha256}");

// Verify HMAC-SHA256 signature
bool isValid = CryptoUtility.VerifyHmacSha256(message, hmacSha256, secretKey);
Console.WriteLine($"Is HMAC-SHA256 signature valid? {isValid}");

// Encrypt a string using AES-256-CBC
var plaintext = "Sensitive data to encrypt";
var encryptionKey = "your_encryption_key_here";
var (ciphertext, iv) = CryptoUtility.EncryptAes256(plaintext, encryptionKey);
Console.WriteLine($"Encrypted '{plaintext}': {ciphertext}, IV: {iv}");

// Decrypt AES-256-CBC ciphertext
var decrypted = CryptoUtility.DecryptAes256(ciphertext, encryptionKey, iv);
Console.WriteLine($"Decrypted: {decrypted}");

// Compare strings securely (prevents timing attacks)
bool stringsAreEqual = CryptoUtility.CompareStringsSecurely("secret", "secret");
Console.WriteLine($"Are strings equal? {stringsAreEqual}");

// Compute file hash for integrity verification
using var fileStream = new FileStream("path/to/file", FileMode.Open);
var fileHash = CryptoUtility.ComputeFileHash(fileStream);
Console.WriteLine($"File hash: {fileHash}");

// Generate password hash (with salt) for secure storage
var password = "my_secure_password";
var (hash, salt) = CryptoUtility.GeneratePasswordHash(password);
Console.WriteLine($"Password hash: {hash}, Salt: {salt}");

// Verify password against stored hash
bool passwordIsValid = CryptoUtility.VerifyPasswordHash(password, hash, salt);
Console.WriteLine($"Is password valid? {passwordIsValid}");
```

<!-- ... rest of README content -->
