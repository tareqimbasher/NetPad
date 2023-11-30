using System;
using System.Collections.Generic;
using System.Linq;

namespace O2Html.Dom;

public class ElementAttribute
{
    public ElementAttribute(Element element, string name, string? value = null)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value?.Trim() ?? string.Empty;
    }

    public Element Element { get; }

    public string Name { get; }

    public string Value { get; private set; }

    public string[] Values => Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public ElementAttribute Set(string? value)
    {
        Value = value ?? string.Empty;
        return this;
    }

    public ElementAttribute Set(IEnumerable<string?> values)
    {
        Value = string.Join(" ", values.Where(v => v != null)).Trim();
        return this;
    }

    public ElementAttribute Clear() => Set(string.Empty);

    public ElementAttribute Append(string? value, bool appendIfExists = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Set(value);
        }
        else
        {
            var values = Values.ToList();
            if (appendIfExists || !values.Contains(value!))
            {
                values.Add(value!);
                Set(values);
            }
        }

        return this;
    }

    public ElementAttribute Remove(string value)
    {
        if (string.IsNullOrWhiteSpace(Value)) return this;
        Set(Values.Where(i => i != value));
        return this;
    }

    public void Delete()
    {
        Element.Attributes.Remove(this);
    }

    public override string ToString()
    {
        return $"{Name}=\"{Value}\"";
    }
}
