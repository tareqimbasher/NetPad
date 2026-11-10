using Xunit;

namespace O2Html.Tests;

public class HtmlSerializerOptionsTests
{
    [Fact]
    public void SettingMaxDepthToZeroUsesTheDefault()
    {
        var options = new HtmlSerializerOptions { MaxDepth = 5 };

        options.MaxDepth = 0;

        Assert.Equal(HtmlSerializerOptions.DefaultMaxDepth, options.MaxDepth);
    }

    [Fact]
    public void SerializersCreatedWithoutOptionsDoNotShareOptions()
    {
        var first = HtmlSerializer.Create();
        var second = HtmlSerializer.Create();

        Assert.NotSame(first.SerializerOptions, second.SerializerOptions);
    }
}
