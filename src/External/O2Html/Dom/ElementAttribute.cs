using System;
using System.Collections.Generic;
using System.Linq;

namespace O2Html.Dom;

public class ElementAttribute
{
    public ElementAttribute(Element element, string name, string? value = null)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        Name = name;
        Value = value?.Trim() ?? string.Empty;
    }

    public Element Element { get; }

    public string Name { get; }

    public string Value { get; private set; }

    public IEnumerable<string> Values => Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);


    public ElementAttribute Set(string? value)
    {
        Value = value?.Trim() ?? string.Empty;
        return this;
    }

    public ElementAttribute Append(string? value, bool appendIfExists = true)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var values = Values.ToList();
            if (appendIfExists || !values.Contains(value!))
            {
                values.Add(value!);
                Set(string.Join(" ", values));
            }
        }

        return this;
    }

    public ElementAttribute Remove(string value)
    {
        if (string.IsNullOrEmpty(Value)) return this;
        Value = string.Join(" ", Value.Split(' ').Where(i => i != value));
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
