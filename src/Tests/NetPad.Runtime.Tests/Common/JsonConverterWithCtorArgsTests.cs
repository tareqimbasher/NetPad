using NetPad.Common;
using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.Common;

public class JsonConverterWithCtorArgsTests
{
    [Fact]
    public void WhenUsed_CreatesConverterInstanceWithArgs()
    {
        string discriminatorName = "someName";
        var attribute = new JsonConverterWithCtorArgs(typeof(JsonInheritanceConverter<Reference>), discriminatorName);

        var converter = attribute.CreateConverter(typeof(JsonInheritanceConverter<Reference>));

        Assert.NotNull(converter);
        Assert.IsType<JsonInheritanceConverter<Reference>>(converter);
        Assert.Equal(discriminatorName, ((JsonInheritanceConverter<Reference>)converter!).DiscriminatorName);
    }
}
