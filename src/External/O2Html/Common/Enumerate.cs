using System.Collections;

namespace O2Html.Common;

public class EnumerationResult
{
    public EnumerationResult(int itemsProcessed, bool collectionLengthExceedsMax)
    {
        ItemsProcessed = itemsProcessed;
        CollectionLengthExceedsMax = collectionLengthExceedsMax;
    }

    /// <summary>
    /// The number of items that were processed (ie. passed to the provided action). For collections that have a length
    /// equalling the provided max or smaller, this number will be the length of the collection. For collections with
    /// a length more than the provided max, this number will be the "Number of enumerated items - 1".
    /// </summary>
    public int ItemsProcessed { get; }

    /// <summary>
    /// Whether the length of the provided collection exceeded the specified max number of items to enumerate.
    /// </summary>
    public bool CollectionLengthExceedsMax { get; }
}

public delegate void ProcessEnumeratedItem<in T>(T o, int itemIndex);

/// <summary>
/// A helper for enumerating collections.
/// </summary>
public static class Enumerate
{
    public static EnumerationResult Max(IEnumerable collection, uint? max, ProcessEnumeratedItem<object?> action)
        => Max<object?>(collection, max, action);

    /// <summary>
    /// Enumerates the specified max number of items of a collection, passing each enumerated item to
    /// the provided delegate, plus 1 additional enumeration to determine if the collection contains more
    /// elements than the specified max count.
    /// </summary>
    /// <param name="collection">The collection to enumerate.</param>
    /// <param name="max">The max number of items to pass to delegate (process).</param>
    /// <param name="action">The delegate to process each item.</param>
    public static EnumerationResult Max<T>(IEnumerable collection, uint? max, ProcessEnumeratedItem<T> action)
    {
        int elementsEnumerated = 0;
        bool collectionLengthExceedsMax = false;

        foreach (T item in collection)
        {
            if (elementsEnumerated >= max)
            {
                collectionLengthExceedsMax = true;
                break;
            }

            elementsEnumerated++;

            action(item, elementsEnumerated - 1);
        }

        return new EnumerationResult(elementsEnumerated, collectionLengthExceedsMax);
    }
}
