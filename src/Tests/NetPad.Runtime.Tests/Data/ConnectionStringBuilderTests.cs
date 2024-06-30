using NetPad.Data;
using Xunit;

namespace NetPad.Runtime.Tests.Data;

public class ConnectionStringBuilderTests
{
    [Fact]
    public void ParsesConnectionStringCorrectly()
    {
        var builder = new ConnectionStringBuilder("Host=some host;Key 1=Value 1; Key 2 = Value 2");

        Assert.Equal("some host", builder["Host"]);
        Assert.Equal("Value 1", builder["Key 1"]);
        Assert.Equal("Value 2", builder["Key 2"]);
    }

    [Fact]
    public void BuildsConnectionStringCorrectly()
    {
        var builder = new ConnectionStringBuilder("Host=some host;Key 1=Value 1; Key 2 = Value 2");

        builder.Augment(new ConnectionStringBuilder("Host=host 2; Key 3 = Value 3"));

        var connectionString = builder.Build();

        Assert.Equal("Host=host 2;Key 1=Value 1;Key 2=Value 2;Key 3=Value 3;", connectionString);
    }
}
