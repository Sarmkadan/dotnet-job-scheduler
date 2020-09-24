# CollectionExtensions

A set of utility extension methods for working with `IEnumerable<T>` and `ICollection<T>` sequences in a functional style, providing safe access, batching, filtering, and transformation operations.

## API

### `Batch<T>(this IEnumerable<T> source, int size)`
Partitions the source sequence into a sequence of subsequences (batches) of the given size.
- **Parameters**:
  - `source`: The sequence to partition.
  - `size`: The maximum number of elements per batch (must be positive).
- **Returns**: An `IEnumerable<IEnumerable<T>>` of batches.
- **Throws**: `ArgumentOutOfRangeException` if `size <= 0`.

### `SafeGetAt<T>(this IReadOnlyList<T> list, int index)`
Safely retrieves the element at the specified index or returns `default` if out of bounds.
- **Parameters**:
  - `list`: The list to access.
  - `index`: The zero-based index.
- **Returns**: The element at `index` or `default(T)` if `index` is invalid.

### `IsEmpty<T>(this IEnumerable<T> source)`
Determines whether the source sequence contains no elements.
- **Parameters**:
  - `source`: The sequence to check.
- **Returns**: `true` if the sequence is empty; otherwise, `false`.

### `HasItems<T>(this IEnumerable<T> source)`
Determines whether the source sequence contains at least one element.
- **Parameters**:
  - `source`: The sequence to check.
- **Returns**: `true` if the sequence has one or more elements; otherwise, `false`.

### `ForEachWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate, Action<T> action)`
Applies an action to each element that satisfies a predicate.
- **Parameters**:
  - `source`: The sequence to enumerate.
  - `predicate`: A function to test each element.
  - `action`: An action to invoke on matching elements.
- **Returns**: The original sequence for method chaining.
- **Throws**: `ArgumentNullException` if `predicate` or `action` is `null`.

### `Random<T>(this IEnumerable<T> source)`
Returns a new sequence with the elements of the source in random order.
- **Parameters**:
  - `source`: The sequence to shuffle.
- **Returns**: A new `IEnumerable<T>` with shuffled elements.
- **Throws**: `ArgumentNullException` if `source` is `null`.

### `CountWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)`
Counts the number of elements in the sequence that satisfy a predicate.
- **Parameters**:
  - `source`: The sequence to count.
  - `predicate`: A function to test each element.
- **Returns**: The number of matching elements.
- **Throws**: `ArgumentNullException` if `predicate` is `null`.

### `ToPage<T>(this IEnumerable<T> source, int pageNumber, int pageSize)`
Slices the source sequence into a single page of results.
- **Parameters**:
  - `source`: The sequence to page.
  - `pageNumber`: The one-based page index (must be positive).
  - `pageSize`: The number of items per page (must be positive).
- **Returns**: A `List<T>` containing the page’s elements.
- **Throws**:
  - `ArgumentOutOfRangeException` if `pageNumber <= 0` or `pageSize <= 0`.
  - `ArgumentNullException` if `source` is `null`.

### `DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)`
Returns distinct elements from the source sequence based on a key selector.
- **Parameters**:
  - `source`: The sequence to filter.
  - `keySelector`: A function to extract the key for comparison.
- **Returns**: An `IEnumerable<T>` of distinct elements.
- **Throws**: `ArgumentNullException` if `source` or `keySelector` is `null`.

### `SafeCast<TResult>(this IEnumerable source)`
Safely casts each element of the source sequence to `TResult`.
- **Parameters**:
  - `source`: The sequence to cast.
- **Returns**: An `IEnumerable<TResult>` of cast elements, skipping any elements that cannot be cast.
- **Throws**: `ArgumentNullException` if `source` is `null`.

### `TakeWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)`
Returns elements from the source sequence until the predicate returns `false`.
- **Parameters**:
  - `source`: The sequence to take from.
  - `predicate`: A function to test each element.
- **Returns**: An `IEnumerable<T>` of elements taken.
- **Throws**: `ArgumentNullException` if `source` or `predicate` is `null`.

### `Chunk<T>(this IEnumerable<T> source, int size)`
Partitions the source sequence into a sequence of `List<T>` chunks of the given size.
- **Parameters**:
  - `source`: The sequence to partition.
  - `size`: The maximum number of elements per chunk (must be positive).
- **Returns**: An `IEnumerable<List<T>>` of chunks.
- **Throws**: `ArgumentOutOfRangeException` if `size <= 0`.

## Usage
