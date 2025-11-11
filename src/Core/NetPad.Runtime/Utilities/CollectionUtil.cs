namespace NetPad.Utilities;

public static class CollectionUtil
{
    public static bool In<T>(this T item, params T[] items) => items.Contains(item);
    public static bool In<T>(this T item, IEnumerable<T> items) => items.Contains(item);
    public static bool In<T>(this T item, IEnumerable<T> items, IEqualityComparer<T> comparer) => items.Contains(item, comparer);

    public static bool NotIn<T>(this T item, params T[] items) => !items.Contains(item);
    public static bool NotIn<T>(this T item, IEnumerable<T> items) => !items.Contains(item);
    public static bool NotIn<T>(this T item, IEnumerable<T> items, IEqualityComparer<T> comparer) => !items.Contains(item, comparer);

    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items) where T : class
    {
        foreach (var item in items)
        {
            hashSet.Add(item);
        }
    }
}
