using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NetPad.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.ExecutionModel.ClientServer;
using NetPad.Presentation;
using NetPad.Runtime.Tests.Presentation;
using O2Html;

namespace NetPad.Runtime.Tests.ExecutionModel.ClientServer;

[Collection(DumpSinkCollection.Name)]
public class ScriptAssemblyUnloadTests : IDisposable
{
    private const string ScriptTypes = """
                                       class Item { public int Id { get; set; } }
                                       enum Color { Red, Green }
                                       class Tagged { public Color Color { get; set; } = Color.Green; }
                                       struct Point { public int X { get; set; } public int Y { get; set; } }
                                       class PairConverter : HtmlConverter
                                       {
                                           public override bool CanConvert(Type type) => type == typeof(KeyValuePair<sbyte, char>);
                                           public override Node WriteHtml<T>(T obj, Type type, SerializationScope scope, HtmlSerializer serializer) => new Element("span");
                                           public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope scope, HtmlSerializer serializer) { }
                                       }
                                       """;

    private readonly string _tempDir;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly TextReader _originalIn = Console.In;
    private readonly List<string> _output = [];

    public ScriptAssemblyUnloadTests()
    {
        // The load context resolves dependencies from the script's own directory. If NetPad.Runtime.dll or
        // O2Html.dll sat beside the script, second copies would load into its context and the dump would not
        // reach the default context's O2Html.
        _tempDir = Path.Combine(Path.GetTempPath(), "NetPad_Tests", nameof(ScriptAssemblyUnloadTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        DumpExtension.UseSink(ClientServerDumpSink.Instance);
        ClientServerDumpSink.Instance.RedirectStdIO(
            str =>
            {
                _output.Add(str);
                return Task.CompletedTask;
            },
            () => null);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
        DumpExtension.UseSink(new NullDumpSink());

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    [Fact]
    public void LoadContextIsCollectedAfterARunThatDumpsOnlyANonScriptValue()
    {
        var path = CompileScript("42.Dump();");

        var contextReference = RunInLoadContext(path);

        Assert.False(IsAliveAfterCollecting(contextReference));
    }

    [Fact]
    public void LoadContextIsCollectedAfterARunThatDumpsAnInstanceOfAScriptDeclaredClass()
    {
        var path = CompileScript("new Item { Id = 1 }.Dump();");

        var contextReference = RunInLoadContext(path);

        Assert.Contains(_output, o => o.Contains("Item"));
        Assert.False(IsAliveAfterCollecting(contextReference));
    }

    [Theory]
    [InlineData("new List<Item> { new() { Id = 1 }, new() { Id = 2 } }.Dump();")]
    [InlineData("new[] { new Item { Id = 1 } }.Dump();")]
    [InlineData("Color.Green.Dump();")]
    [InlineData("new Tagged().Dump();")]
    [InlineData("new Point { X = 1, Y = 2 }.Dump();")]
    [InlineData("new { Name = \"a\", Id = 1 }.Dump();")]
    [InlineData("new Dictionary<string, Item> { [\"a\"] = new() { Id = 1 } }.Dump();")]
    public void LoadContextIsCollectedAfterARunThatDumpsAScriptType(string statement)
    {
        var path = CompileScript(statement);

        var contextReference = RunInLoadContext(path);

        Assert.NotEmpty(_output);
        Assert.False(IsAliveAfterCollecting(contextReference));
    }

    [Fact]
    public void LoadContextIsCollectedAfterARunThatSerializesWithItsOwnConverter()
    {
        var path = CompileScript(
            "HtmlSerializer.Serialize(new KeyValuePair<sbyte, char>(1, 'a'), new HtmlSerializerOptions { Converters = { new PairConverter() } });");

        var contextReference = RunInLoadContext(path);

        Assert.False(IsAliveAfterCollecting(contextReference));
    }

    [Fact]
    public void LoadContextIsCollectedAfterARunThatAddsItsOwnConverterToASerializerCreatedWithoutOptions()
    {
        var path = CompileScript("HtmlSerializer.Create().SerializerOptions.Converters.Add(new PairConverter());");

        var contextReference = RunInLoadContext(path);

        Assert.False(IsAliveAfterCollecting(contextReference));
    }

    private string CompileScript(string statement)
    {
        var code = $"""
                    using System;
                    using System.Collections.Generic;
                    using NetPad.Presentation;
                    using O2Html;
                    using O2Html.Dom;
                    using O2Html.Dom.Elements;

                    {statement}

                    {ScriptTypes}
                    """;

        var compiler = new CSharpCodeCompiler(
            new DotNetInfo(new Settings(), NullLogger<DotNetInfo>.Instance),
            new CodeAnalysisService());

        var result = compiler.Compile(new CompilationInput(
            code,
            DotNetFrameworkVersion.DotNet10,
            assemblyFileReferences: [typeof(HtmlSerializer).Assembly.Location]));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));

        var path = Path.Combine(_tempDir, "NetPadScript.dll");
        File.WriteAllBytes(path, result.AssemblyBytes);
        return path;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunInLoadContext(string path)
    {
        using var context = ScriptAssemblyExecutor.CreateLoadContext(path, []);

        ScriptAssemblyExecutor.InvokeEntryPoint(context, path);

        return new WeakReference(context);
    }

    private static bool IsAliveAfterCollecting(WeakReference reference)
    {
        for (int i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return reference.IsAlive;
    }
}
