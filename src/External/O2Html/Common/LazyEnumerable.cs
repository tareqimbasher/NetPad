using System;
using System.Collections;
using System.Collections.Generic;

namespace O2Html.Common;

internal class LazyEnumerationResult
{
    public int ElementsEnumerated { get; set; }
    public bool CollectionLengthExceedsMax { get; set; }
}

internal static class LazyEnumerable
{
    public static LazyEnumerationResult Enumerate<T>(IEnumerable<T> collection, int? maxItemsToEnumerate, Action<T?, int> action)
    {
        var result = new LazyEnumerationResult();

        int currentElementIndex = -1;

        foreach (T element in collection)
        {
            ++currentElementIndex;
            if (result.ElementsEnumerated + 1 > maxItemsToEnumerate)
            {
                result.CollectionLengthExceedsMax = true;
                break;
            }

            action(element, currentElementIndex);
            ++result.ElementsEnumerated;
        }

        return result;
    }

    public static LazyEnumerationResult Enumerate(IEnumerable collection, int? maxItemsToEnumerate, Action<object?, int> action)
    {
        return Enumerate<object>(collection, maxItemsToEnumerate, action);
    }

    public static LazyEnumerationResult Enumerate<T>(IEnumerable collection, int? maxItemsToEnumerate, Action<T?, int> action)
    {
        var result = new LazyEnumerationResult();

        int currentElementIndex = -1;

        foreach (T element in collection)
        {
            ++currentElementIndex;
            if (result.ElementsEnumerated + 1 > maxItemsToEnumerate)
            {
                result.CollectionLengthExceedsMax = true;
                break;
            }


            action(element, currentElementIndex);
            ++result.ElementsEnumerated;
        }

        return result;
    }
}
