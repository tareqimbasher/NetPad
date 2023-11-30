using System;
using System.Collections;
using System.Collections.Generic;

namespace O2Html.Common;

public class LazyEnumerationResult
{
    public int ElementsEnumerated { get; set; }
    public bool CollectionLengthExceedsMax { get; set; }
}

public static class LazyEnumerable
{
    public static LazyEnumerationResult Enumerate<T>(IEnumerable<T> collection, uint? maxItemsToEnumerate, Action<T?, uint> action)
    {
        var result = new LazyEnumerationResult();

        uint currentElementIndex = 0;

        foreach (T element in collection)
        {
            if (result.ElementsEnumerated + 1 > maxItemsToEnumerate)
            {
                result.CollectionLengthExceedsMax = true;
                break;
            }

            action(element, currentElementIndex);
            ++result.ElementsEnumerated;
            ++currentElementIndex;
        }

        return result;
    }

    public static LazyEnumerationResult Enumerate(IEnumerable collection, uint? maxItemsToEnumerate, Action<object?, uint> action)
    {
        return Enumerate<object>(collection, maxItemsToEnumerate, action);
    }

    public static LazyEnumerationResult Enumerate<T>(IEnumerable collection, uint? maxItemsToEnumerate, Action<T?, uint> action)
    {
        var result = new LazyEnumerationResult();

        uint currentElementIndex = 0;

        foreach (T element in collection)
        {
            if (result.ElementsEnumerated + 1 > maxItemsToEnumerate)
            {
                result.CollectionLengthExceedsMax = true;
                break;
            }


            action(element, currentElementIndex);
            ++result.ElementsEnumerated;
            ++currentElementIndex;
        }

        return result;
    }
}
