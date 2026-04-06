namespace Amp.Core.Common.Extensions;

public static class EnumerableExtensions
{
    /// <summary>Returns true when the collection is null or contains no elements.</summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();

    /// <summary>Performs an action for each element. Useful for side-effect iteration.</summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source) action(item);
    }

    /// <summary>Splits a sequence into fixed-size chunks.</summary>
    public static IEnumerable<IReadOnlyList<T>> Chunk<T>(this IEnumerable<T> source, int size)
    {
        var chunk = new List<T>(size);
        foreach (var item in source)
        {
            chunk.Add(item);
            if (chunk.Count == size)
            {
                yield return chunk.AsReadOnly();
                chunk = new List<T>(size);
            }
        }
        if (chunk.Count > 0) yield return chunk.AsReadOnly();
    }

    /// <summary>Returns distinct elements by a key selector.</summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
            if (seen.Add(keySelector(item)))
                yield return item;
    }

    /// <summary>Converts a single item to a one-element list.</summary>
    public static List<T> ToSingleItemList<T>(this T item) => [item];

    /// <summary>Returns a page of results from a sequence.</summary>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int page, int pageSize) =>
        source.Skip((page - 1) * pageSize).Take(pageSize);
}
