using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common
{
    /// <summary>
    /// Defines the class as inheritance base class and adds a discriminator property to the serialized object.
    /// <remarks>
    /// Built using this as a template:
    /// https://github.com/RicoSuter/NJsonSchema/blob/master/src/NJsonSchema/Converters/JsonInheritanceConverter.cs
    /// </remarks>
    /// </summary>
    public class JsonInheritanceConverter<T> : JsonConverter<T> where T : class
    {
        /// <summary>Gets the default discriminator name.</summary>
        public const string DefaultDiscriminatorName = "discriminator";

        private readonly Type? _baseType;
        private readonly string _discriminatorName;
        private readonly bool _readTypeProperty;

        [ThreadStatic]
        // We are currently not reading this field, only setting it.
#pragma warning disable CS0414
        private static bool _isReading;
#pragma warning restore CS0414

        [ThreadStatic]
        // We are currently not reading this field, only setting it.
#pragma warning disable CS0414
        private static bool _isWriting;
#pragma warning restore CS0414

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class.</summary>
        public JsonInheritanceConverter()
            : this(DefaultDiscriminatorName, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class.</summary>
        /// <param name="discriminatorName">The discriminator.</param>
        public JsonInheritanceConverter(string discriminatorName)
            : this(discriminatorName, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class.</summary>
        /// <param name="discriminatorName">The discriminator property name.</param>
        /// <param name="readTypeProperty">Read the $type property to determine the type
        /// (fallback, should not be used as it might lead to security problems).</param>
        public JsonInheritanceConverter(string discriminatorName, bool readTypeProperty)
        {
            _discriminatorName = discriminatorName;
            _readTypeProperty = readTypeProperty;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class which only applies for the given base type.</summary>
        /// <remarks>Use this constructor for global registered converters (not defined on class).</remarks>
        /// <param name="baseType">The base type.</param>
        public JsonInheritanceConverter(Type baseType)
            : this(baseType, DefaultDiscriminatorName, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class which only applies for the given base type.</summary>
        /// <remarks>Use this constructor for global registered converters (not defined on class).</remarks>
        /// <param name="baseType">The base type.</param>
        /// <param name="discriminatorName">The discriminator.</param>
        public JsonInheritanceConverter(Type baseType, string discriminatorName)
            : this(baseType, discriminatorName, false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter{T}"/> class which only applies for the given base type.</summary>
        /// <remarks>Use this constructor for global registered converters (not defined on class).</remarks>
        /// <param name="baseType">The base type.</param>
        /// <param name="discriminatorName">The discriminator.</param>
        /// <param name="readTypeProperty">Read the $type property to determine the type
        /// (fallback, should not be used as it might lead to security problems).</param>
        public JsonInheritanceConverter(Type baseType, string discriminatorName, bool readTypeProperty)
            : this(discriminatorName, readTypeProperty)
        {
            _baseType = baseType;
        }

        /// <summary>Gets the discriminator property name.</summary>
        public virtual string DiscriminatorName => _discriminatorName;

        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            if (_baseType == null) return true;

            var type = objectType;
            while (type != null)
            {
                if (type == _baseType)
                {
                    return true;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return false;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            try
            {
                _isWriting = true;

                using JsonDocument document = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), options));
                WriteElement(writer, document.RootElement, GetDiscriminatorValue(value.GetType()));
            }
            finally
            {
                _isWriting = false;
            }
        }

        private void WriteElement(Utf8JsonWriter writer, JsonElement element, string? discriminatorType = null)
        {
            writer.WriteStartObject();

            if (discriminatorType != null)
            {
                writer.WritePropertyName(_discriminatorName);
                writer.WriteStringValue(discriminatorType);
            }

            foreach (var property in element.EnumerateObject())
            {
                WriteProperty(writer, property);
            }

            writer.WriteEndObject();
        }

        private void WriteProperty(Utf8JsonWriter writer, JsonProperty property)
        {
            // Null and undefined are written as is
            if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
            {
                property.WriteTo(writer);
                return;
            }

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartObject();

                foreach (var innerProp in property.Value.EnumerateObject())
                {
                    WriteProperty(writer, innerProp);
                }

                writer.WriteEndObject();
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartArray();

                foreach (var item in property.Value.EnumerateArray())
                {
                    WriteElement(writer, item);
                }

                writer.WriteEndArray();
            }
            else
                property.WriteTo(writer);
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check for null values
            if (reader.TokenType == JsonTokenType.Null) return null;

            // Copy the current state from reader (it's a struct)
            var readerAtStart = reader;

            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            var jsonObject = jsonDocument.RootElement;

            var discriminator = jsonObject.GetProperty(_discriminatorName).GetString();

            Type targetType = string.IsNullOrWhiteSpace(_discriminatorName) ?
                    typeToConvert :
                    GetDiscriminatorType(jsonObject, typeToConvert, discriminator!);

            try
            {
                _isReading = true;
                return System.Text.Json.JsonSerializer.Deserialize(ref readerAtStart, targetType, options) as T;
            }
            finally
            {
                _isReading = false;
            }
        }

        /// <summary>Gets the discriminator value for the given type.</summary>
        /// <param name="type">The object type.</param>
        /// <returns>The discriminator value.</returns>
        public virtual string GetDiscriminatorValue(Type type)
        {
            var jsonInheritanceAttributeDiscriminator = GetSubtypeDiscriminator(type);
            if (jsonInheritanceAttributeDiscriminator != null)
            {
                return jsonInheritanceAttributeDiscriminator;
            }

            return type.Name;
        }

        /// <summary>Gets the type for the given discriminator value.</summary>
        /// <param name="jsonObject">The JSON object.</param>
        /// <param name="objectType">The object (base) type.</param>
        /// <param name="discriminatorValue">The discriminator value.</param>
        /// <returns></returns>
        protected virtual Type GetDiscriminatorType(JsonElement jsonObject, Type objectType, string discriminatorValue)
        {
            var jsonInheritanceAttributeSubtype = GetObjectSubtype(objectType, discriminatorValue);
            if (jsonInheritanceAttributeSubtype != null)
            {
                return jsonInheritanceAttributeSubtype;
            }

            if (objectType.Name == discriminatorValue)
            {
                return objectType;
            }

            var knownTypeAttributesSubtype = GetSubtypeFromKnownTypeAttributes(objectType, discriminatorValue);
            if (knownTypeAttributesSubtype != null)
            {
                return knownTypeAttributesSubtype;
            }

            var typeName = objectType.Namespace + "." + discriminatorValue;
            var subtype = objectType.GetTypeInfo().Assembly.GetType(typeName);
            if (subtype != null)
            {
                return subtype;
            }

            if (_readTypeProperty)
            {
                var typeInfo = jsonObject.GetProperty("$type").GetString();
                if (typeInfo != null)
                {
                    return Type.GetType(typeInfo) ??
                           throw new Exception($"Could not load type {typeInfo}");
                }
            }

            throw new InvalidOperationException("Could not find subtype of '" + objectType.Name + "' with discriminator '" + discriminatorValue + "'.");
        }

        private Type? GetSubtypeFromKnownTypeAttributes(Type objectType, string discriminator)
        {
            var type = objectType;
            do
            {
                var knownTypeAttributes = type.GetTypeInfo().GetCustomAttributes(false)
                    .Where(a => a.GetType().Name == "KnownTypeAttribute");
                foreach (dynamic attribute in knownTypeAttributes)
                {
                    if (attribute.Type != null && attribute.Type.Name == discriminator)
                    {
                        return attribute.Type;
                    }
                    else if (attribute.MethodName != null)
                    {
                        var method = type.GetRuntimeMethod((string)attribute.MethodName, new Type[0]);
                        if (method != null)
                        {
                            var types = (IEnumerable<Type>?)method.Invoke(null, new object[0]);

                            if (types != null)
                            {
                                foreach (var knownType in types)
                                {
                                    if (knownType.Name == discriminator)
                                    {
                                        return knownType;
                                    }
                                }
                            }

                            return null;
                        }
                    }
                }
                type = type.GetTypeInfo().BaseType;
            } while (type != null);

            return null;
        }

        private static Type? GetObjectSubtype(Type baseType, string discriminatorName)
        {
            return null;

            // var jsonInheritanceAttributes = baseType
            //     .GetTypeInfo()
            //     .GetCustomAttributes(true)
            //     .OfType<JsonInheritanceConverter();
            //
            // return jsonInheritanceAttributes.SingleOrDefault(a => a. == discriminatorName)?.Type;
        }

        private static string? GetSubtypeDiscriminator(Type objectType)
        {
            return null;
            // var jsonInheritanceAttributes = objectType
            //     .GetTypeInfo()
            //     .GetCustomAttributes(true)
            //     .OfType<JsonInheritanceConverter();
            //
            // return jsonInheritanceAttributes.SingleOrDefault(a => a.Type == objectType)?.Key;
        }
    }
}
