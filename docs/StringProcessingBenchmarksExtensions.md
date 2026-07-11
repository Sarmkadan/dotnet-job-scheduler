# StringProcessingBenchmarksExtensions

A utility class providing commonly-needed string processing methods optimized for performance and correctness in .NET applications. These methods are designed for scenarios where strings must be transformed for URLs, logs, or display while preserving thread safety and avoiding allocations where possible.

## API

### `ToSlugUrlFriendly(string input)`

Converts a string into a URL-friendly slug by normalizing characters, replacing spaces with hyphens, and removing invalid characters.

- **Parameters**
  - `input` (string): The string to convert. Can be null or empty.
- **Return value**
  - A new string representing the slug, or null if the input is null.
- **Exceptions**
  - Throws `ArgumentNullException` if `input` is null (only when explicitly passed as non-null; null input returns null).

### `JsonEscapeFull(string input)`

Escapes all characters in a string that require escaping in JSON according to RFC 8259, including control characters and quotes.

- **Parameters**
  - `input` (string): The string to escape. Can be null.
- **Return value**
  - A new string with all necessary JSON escape sequences applied, or null if the input is null.

### `TruncateWithEllipsis(string input, int maxLength)`

Truncates a string to a specified maximum length and appends an ellipsis ("…") if truncation occurs.

- **Parameters**
  - `input` (string): The string to truncate. Can be null or empty.
  - `maxLength` (int): The maximum allowed length of the result, including the ellipsis. Must be ≥ 1.
- **Return value**
  - A new string truncated to `maxLength` with "…" appended if truncation occurred, or null if the input is null.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `maxLength` < 1.

### `MaskSensitive(string input, int keepLeft, int keepRight)`

Masks sensitive parts of a string by replacing the middle portion with asterisks, preserving the first and last characters for readability.

- **Parameters**
  - `input` (string): The string to mask. Can be null or empty.
  - `keepLeft` (int): Number of characters to preserve from the start. Must be ≥ 0.
  - `keepRight` (int): Number of characters to preserve from the end. Must be ≥ 0.
- **Return value**
  - A new string with the middle portion masked, or null if the input is null.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `keepLeft` or `keepRight` is negative.
  - Throws `ArgumentException` if `keepLeft + keepRight` exceeds the length of `input`.

## Usage
