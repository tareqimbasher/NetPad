using System.Text.Json.Serialization;

namespace NetPad.Common;

public class JsonConverterWithCtorArgs(Type? converterType, params object?[] ctorArgs) : JsonConverterAttribute
{
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (converterType == null)
            return base.CreateConverter(typeToConvert);

        return Activator.CreateInstance(converterType, ctorArgs) as JsonConverter;
    }
}
