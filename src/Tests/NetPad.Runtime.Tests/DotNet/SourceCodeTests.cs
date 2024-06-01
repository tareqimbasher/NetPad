using NetPad.DotNet;
using Xunit;

namespace NetPad.Runtime.Tests.DotNet;

public class SourceCodeTests
{
    private const string Code = """
                                using System;
                                using System.Text.Json;
                                using System.Text
                                .Json;
                                using System.Threading
                                .Tasks;
                                using enc = System.Text.Encoding;
                                using enc2 = System.Text
                                .Encoding;

                                namespace MyApp.Utils;

                                public class Car
                                {
                                     public string Name { get; }
                                }

                                public enum Color
                                {
                                     Red, Blue, Green
                                }
                                """;

    [Fact]
    public void ParsesUsingsCorrectly()
    {
        var sourceCode = SourceCode.Parse(Code);

        Assert.Equal(
            [
                "System",
                "System.Text.Json",
                "System.Threading.Tasks",
                "enc = System.Text.Encoding",
                "enc2 = System.Text.Encoding",
            ],
            sourceCode.Usings.Select(x => x.Value));
    }
}
