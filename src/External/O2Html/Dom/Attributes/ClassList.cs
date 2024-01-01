using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace O2Html.Dom.Attributes;

/// <summary>
/// A collection used to interact with an Element's class list.
/// </summary>
public class ClassList : IList<string>
{
    private readonly Element _element;

    public ClassList(Element element)
    {
        _element = element;
    }

    public int Count => ClassAttribute?.Values.Length ?? 0;
    public bool IsReadOnly => false;

    private ElementAttribute? ClassAttribute => _element.GetAttribute("class");
    private ElementAttribute ClassAttributeOrCreate => _element.GetOrAddAttribute("class");

    public IEnumerator<string> GetEnumerator()
    {
        var enumerable = (IEnumerable<string>)(ClassAttribute?.Values ?? Array.Empty<string>());

        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(string item)
    {
        ClassAttributeOrCreate.Append(item, false);
    }

    public void Clear()
    {
        ClassAttribute?.Clear();
    }

    public bool Contains(string item)
    {
        return ClassAttribute?.Values.Contains(item) == true;
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        ClassAttribute?.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        var existed = Contains(item);

        ClassAttribute?.Remove(item);

        return existed;
    }

    public int IndexOf(string item)
    {
        var classAttribute = ClassAttribute;

        return classAttribute?.IsEmpty != false ? -1 : Array.IndexOf(classAttribute.Values, item);
    }

    public void Insert(int index, string item)
    {
        var classAttribute = ClassAttributeOrCreate;

        var values = classAttribute.Values.ToList();

        values.Insert(index, item);

        classAttribute.Set(values);
    }

    public void RemoveAt(int index)
    {
        var classAttribute = ClassAttributeOrCreate;

        var values = classAttribute.Values.ToList();

        values.RemoveAt(index);

        classAttribute.Set(values);
    }

    public string this[int index]
    {
        get => ClassAttribute?.Values[index] ?? string.Empty;
        set
        {
            var classAttribute = ClassAttributeOrCreate;
            var values = classAttribute.Values;

            values[index] = value;

            classAttribute.Set(values);
        }
    }
}
