# StringExtensions

Utility class providing common string manipulation and validation extensions for .NET applications.

## API

### `public static string ToSha256(this string input)`

Computes the SHA-256 hash of the input string and returns the hexadecimal representation. Returns `null` if the input is `null`.

- **Parameters**: `input` – the string to hash.
- **Returns**: Hexadecimal SHA-256 hash of the input, or `null` if input is `null`.
- **Throws**: `ArgumentNullException` if `input` is `null` (only when explicitly passed as a non-null value; does not throw on `null` input due to extension method semantics).

---

### `public static string Truncate(this string input, int maxLength)`

Truncates the string to the specified maximum length and appends an ellipsis if truncation occurs. Returns `null` if the input is `null`.

- **Parameters**:
  - `input` – the string to truncate.
  - `maxLength` – the maximum allowed length of the result (must be ≥ 0).
- **Returns**: Truncated string with ellipsis if truncated, otherwise the original string; `null` if input is `null`.
- **Throws**: `ArgumentOutOfRangeException` if `maxLength` < 0.

---

### `public static string ToSlug(this string input)`

Converts the input string into a URL-friendly slug by normalizing Unicode characters, removing diacritics, and replacing spaces and punctuation with hyphens. Returns `null` if the input is `null`.

- **Parameters**: `input` – the string to convert.
- **Returns**: Lowercase slug with hyphens, or `null` if input is `null`.

---

### `public static string JsonEscape(this string input)`

Escapes the input string for safe inclusion in JSON by escaping quotes, backslashes, and control characters. Returns `null` if the input is `null`.

- **Parameters**: `input` – the string to escape.
- **Returns**: JSON-escaped string, or `null` if input is `null`.

---

### `public static bool IsValidGuid(this string input)`

Determines whether the input string represents a valid GUID in any of the common formats (with or without braces, hyphens, or uppercase). Returns `false` if the input is `null`.

- **Parameters**: `input` – the string to validate.
- **Returns**: `true` if the string is a valid GUID; otherwise, `false`.

---

### `public static bool IsValidEmail(this string input)`

Determines whether the input string is a syntactically valid email address according to RFC 5322 standards. Returns `false` if the input is `null`.

- **Parameters**: `input` – the string to validate.
- **Returns**: `true` if the string is a valid email; otherwise, `false`.

---

### `public static string Repeat(this string input, int count)`

Repeats the input string the specified number of times. Returns `null` if the input is `null`.

- **Parameters**:
  - `input` – the string to repeat.
  - `count` – the number of repetitions (must be ≥ 0).
- **Returns**: Repeated string, or `null` if input is `null`.
- **Throws**: `ArgumentOutOfRangeException` if `count` < 0.

---
### `public static string Mask(this string input, char maskChar = '*', int unmaskedPrefix = 2, int unmaskedSuffix = 2)`

Masks all but the first `unmaskedPrefix` and last `unmaskedSuffix` characters of the input string with the specified mask character. Returns `null` if the input is `null`.

- **Parameters**:
  - `input` – the string to mask.
  - `maskChar` – the character used for masking (default `'*'`).
  - `unmaskedPrefix` – number of leading characters to leave unmasked (default 2).
  - `unmaskedSuffix` – number of trailing characters to leave unmasked (default 2).
- **Returns**: Masked string, or `null` if input is `null`.
- **Throws**: `ArgumentOutOfRangeException` if `unmaskedPrefix` < 0 or `unmaskedSuffix` < 0 or if the total unmasked length exceeds the input length.

---
### `public static List<string> ToList(this string input, char separator = ',')`

Splits the input string into a list of substrings using the specified separator. Returns `null` if the input is `null`.

- **Parameters**:
  - `input` – the string to split.
  - `separator` – the character used to split the string (default `','`).
- **Returns**: List of substrings, or `null` if input is `null`.

---
### `public static bool IsAlphanumericWithUnderscore(this string input)`

Determines whether the input string contains only alphanumeric characters and underscores. Returns `false` if the input is `null`.

- **Parameters**: `input` – the string to validate.
- **Returns**: `true` if the string is alphanumeric with underscores; otherwise, `false`.

## Usage
