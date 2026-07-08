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
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
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
    /// Safely gets item at index or returns null if index is out of bounds.
    /// Prevents exceptions in concurrent access scenarios.
    /// </summary>
    public static T? SafeGetAt<T>(this IList<T>? list, int index) where T : class
    {
        if (list is null || index < 0 || index >= list.Count)
            return null;

        return list[index];
    }

    /// <summary>
    /// Checks if collection is empty without throwing.
    /// More readable alternative to !list.Any().
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    /// Checks if collection is not empty.
    /// Opposite of IsEmpty for clarity.
    /// </summary>
    public static bool HasItems<T>(this IEnumerable<T>? source)
    {
        return source?.Any() ?? false;
    }

    /// <summary>
    /// Applies action to each item if condition is met.
    /// Fluent alternative to Where + ForEach.
    /// </summary>
    public static IEnumerable<T> ForEachWhere<T>(this IEnumerable<T> items, Func<T, bool> predicate, Action<T> action)
    {
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
    public static IEnumerable<T> Random<T>(this IEnumerable<T> source, int count)
    {
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
    /// Counts items matching predicate without creating intermediate collections.
    /// More efficient than Where + Count for large collections.
    /// </summary>
    public static int CountWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return source.Where(predicate).Count();
    }

    /// <summary>
    /// Converts list to page based on page number and size.
    /// Centralizes pagination logic for consistency.
    /// </summary>
    public static List<T> ToPage<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 10;

        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Removes duplicates based on key selector.
    /// Preserves first occurrence of each key.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
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
    public static IEnumerable<TResult> SafeCast<TResult>(this System.Collections.IEnumerable source) where TResult : class
    {
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
    public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            if (!predicate(item))
                yield break;
            yield return item;
        }
    }

    /// <summary>
    /// Groups items into fixed-size groups maintaining order.
    /// Different from Batch in that it yields groups as they fill.
    /// </summary>
    public static IEnumerable<List<T>> Chunk<T>(this IEnumerable<T> items, int chunkSize)
    {
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
}
