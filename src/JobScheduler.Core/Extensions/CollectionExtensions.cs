#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Extensions;

/// <summary>
/// Collection extension methods for common list and enumerable operations.
/// Provides batching, filtering, and transformation utilities used in job scheduling.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Batches enumerable into chunks of specified size.
    /// WHY: Prevents memory exhaustion when processing large result sets in pagination scenarios.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="batchSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));

        var batch = new List<T>(batchSize);
        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Safely gets item at index or returns default if index is out of bounds.
    /// Prevents exceptions in concurrent access scenarios.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
    public static T? SafeGetAt<T>(this IList<T>? list, int index)
    {
        ArgumentNullException.ThrowIfNull(list);
        return index >= 0 && index < list.Count ? list[index] : default;
    }

    /// <summary>
    /// Checks if collection is empty without throwing.
    /// More readable alternative to !list.Any().
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static bool IsEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    /// Checks if collection is not empty.
    /// Opposite of IsEmpty for clarity.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static bool HasItems<T>(this IEnumerable<T>? source)
    {
        return source?.Any() ?? false;
    }

    /// <summary>
    /// Applies action to each item if condition is met.
    /// Fluent alternative to Where + ForEach.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
    public static IEnumerable<T> ForEachWhere<T>(
        this IEnumerable<T> items,
        Func<T, bool> predicate,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in items.Where(predicate))
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Gets specified number of random items from collection.
    /// Useful for sampling job executions for analysis.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="count"/> is less than zero.</exception>
    public static IEnumerable<T> Random<T>(this IEnumerable<T> source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (count < 0)
            throw new ArgumentException("Count must be non-negative", nameof(count));

        var items = source.ToList();
        var rng = new System.Random();

        for (int i = 0; i < count && items.Count > 0; i++)
        {
            var index = rng.Next(items.Count);
            yield return items[index];
            items.RemoveAt(index);
        }
    }

    /// <summary>
    /// Groups items into fixed-size groups maintaining order.
    /// Different from Batch in that it yields groups as they fill.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="chunkSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<List<T>> Chunk<T>(this IEnumerable<T> items, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than zero", nameof(chunkSize));

        var chunk = new List<T>(chunkSize);
        foreach (var item in items)
        {
            chunk.Add(item);
            if (chunk.Count == chunkSize)
            {
                yield return new List<T>(chunk);
                chunk.Clear();
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>
    /// Groups items into chunks of specified size.
    /// Preserves first occurrence of each key.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is null.</exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
                yield return item;
        }
    }

    /// <summary>
    /// Safely casts collection without throwing on type mismatch.
    /// Returns empty collection if cast fails.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IEnumerable<TResult> SafeCast<TResult>(this System.Collections.IEnumerable source)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (item is TResult result)
                yield return result;
        }
    }

    /// <summary>
    /// Returns items from start until predicate becomes false.
    /// Useful for processing job executions until a condition changes.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public static IEnumerable<T> TakeWhile<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (!predicate(item))
                yield break;
            yield return item;
        }
    }

    /// <summary>
    /// Converts list to page based on page number and size.
    /// Centralizes pagination logic for consistency.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="pageSize"/> is less than one.</exception>
    public static List<T> ToPage<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (pageSize < 1)
            throw new ArgumentException("Page size must be at least one", nameof(pageSize));

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);

        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}