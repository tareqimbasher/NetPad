using System;
using System.Text.Json.Serialization;

namespace NetPad.Common;

public class JsonConverterWithCtorArgs : JsonConverterAttribute
{
    private readonly Type? _converterType;
    private readonly object?[] _ctorArgs;

    public JsonConverterWithCtorArgs(Type? converterType, params object?[] ctorArgs)
    {
        _converterType = converterType;
        _ctorArgs = ctorArgs;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (_converterType == null)
            return base.CreateConverter(typeToConvert);

        return Activator.CreateInstance(_converterType, _ctorArgs) as JsonConverter;
    }
}
