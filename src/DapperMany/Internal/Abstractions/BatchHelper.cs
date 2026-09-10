namespace DapperMany.Internal.Abstractions;

/// <summary>
/// Helper utilities for batching operations efficiently.
/// </summary>
internal static class BatchHelper
{
    /// <summary>
    /// Batches a list into fixed-size chunks using GetRange (efficient for List&lt;T&gt;).
    /// Avoids the allocations of Skip().Take().ToList() by using list indexing directly.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Source list to batch</param>
    /// <param name="batchSize">Number of items per batch</param>
    /// <returns>Enumerable of batch lists</returns>
    public static IEnumerable<List<T>> Batch<T>(List<T> source, int batchSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentException("Batch size must be positive.", nameof(batchSize));

        for (int i = 0; i < source.Count; i += batchSize)
        {
            var count = Math.Min(batchSize, source.Count - i);
            yield return source.GetRange(i, count);
        }
    }

    /// <summary>
    /// Batches any enumerable into fixed-size chunks using a forward-only scan.
    /// Less efficient than GetRange but works with any IEnumerable&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">Source enumerable to batch</param>
    /// <param name="batchSize">Number of items per batch</param>
    /// <returns>Enumerable of batch lists</returns>
    public static IEnumerable<List<T>> BatchEnumerable<T>(IEnumerable<T> source, int batchSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentException("Batch size must be positive.", nameof(batchSize));

        var batch = new List<T>();
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>();
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }
}
