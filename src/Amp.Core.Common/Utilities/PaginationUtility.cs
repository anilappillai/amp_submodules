using Amp.Core.Common.BaseClasses;

namespace Amp.Core.Common.Utilities;

/// <summary>
/// Helpers for computing pagination metadata and clamping page parameters.
/// </summary>
public static class PaginationUtility
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;

    /// <summary>Clamps the page number to a valid positive integer.</summary>
    public static int ClampPage(int page) => Math.Max(1, page);

    /// <summary>Clamps the page size between 1 and <see cref="MaxPageSize"/>.</summary>
    public static int ClampPageSize(int pageSize) =>
        Math.Clamp(pageSize, 1, MaxPageSize);

    /// <summary>Builds a <see cref="PagedResult{T}"/> from a full in-memory list.</summary>
    public static PagedResult<T> Paginate<T>(IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        page = ClampPage(page);
        pageSize = ClampPageSize(pageSize);
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList().AsReadOnly();
        return new PagedResult<T>(items, list.Count, page, pageSize);
    }
}
