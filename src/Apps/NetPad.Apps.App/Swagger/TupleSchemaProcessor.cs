using System.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;

namespace NetPad.Swagger;

/// <summary>
/// A schema processor that converts CLR <c>System.ValueTuple&lt;…&gt;</c> types into JSON‑Schema object definitions
/// with <c>item1</c>, <c>item2</c>, … properties.
/// </summary>
/// <remarks>
/// This was added after upgrading NJsonSchema from v10 to v11. After the upgrade, tuples were generated in TypeScript
/// as empty classes. Before it would generate item1, item2... properties for each item in the tuple. This restores that
/// behavior.
/// </remarks>
public class TupleSchemaProcessor : ISchemaProcessor
{
    private readonly NullabilityInfoContext _nullContext = new();

    public void Process(SchemaProcessorContext context)
    {
        var type = context.ContextualType.Type;

        // Only process ValueTuple<...> types
        if (!type.IsGenericType || type.FullName?.StartsWith("System.ValueTuple") != true)
        {
            return;
        }

        var schema = context.Schema;
        var generator = context.Generator;
        var resolver = context.Resolver;

        // Turn the schema into an object if it isn't already
        schema.Type = JsonObjectType.Object;
        schema.Items.Clear();
        schema.Properties.Clear();

        var args = type.GetGenericArguments();
        for (var i = 0; i < args.Length; i++)
        {
            // Reflect on the ValueTuple<T1,…,Tn>.Item{i+1} field and find if its nullable
            var fieldInfo = type.GetField($"Item{i + 1}")!;
            bool isNullable;
            if (fieldInfo.FieldType.IsValueType)
            {
                isNullable = Nullable.GetUnderlyingType(fieldInfo.FieldType) != null;
            }
            else
            {
                var nullInfo = _nullContext.Create(fieldInfo);
                isNullable = nullInfo.ReadState == NullabilityState.Nullable;
            }

            // Generate the inner schema and get the "actual" (i.e. un‑$ref’ed) version
            var inner = generator.Generate(args[i], resolver).ActualSchema;

            // Generate a property for the tuple item
            var prop = new JsonSchemaProperty
            {
                Id = inner.Id,
                Type = inner.Type,
                Format = inner.Format,
                Title = inner.Title,
                Description = inner.Description,
                IsNullableRaw = isNullable,
                IsRequired = !isNullable,
                Reference = inner.Reference,
                AllowAdditionalProperties = inner.AllowAdditionalProperties,
                Default = inner.Default,
                Example = inner.Example
            };

            schema.Properties.Add($"item{i + 1}", prop);
        }
    }
}
