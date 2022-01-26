using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using NetPad.Common;

namespace NetPad.Scripts
{
    // This attribute is only used for NSwag polymorphism. Haven't figured out how to use our own STJ-based JsonInheritanceConverter
    // and have NSwag use that when generating its schema.
    [Newtonsoft.Json.JsonConverter(typeof(NJsonSchema.Converters.JsonInheritanceConverter), "discriminator")]

    [JsonConverter(typeof(JsonInheritanceConverter<Reference>))]
    [KnownType(typeof(AssemblyReference))]
    [KnownType(typeof(PackageReference))]
    public abstract class Reference
    {
        protected Reference(string title)
        {
            Title = title;
        }

        public string Title { get; }
        public abstract void EnsureValid();
    }
}
