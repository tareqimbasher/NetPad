using System.Collections;
using System.Runtime.CompilerServices;
using NetPad.ExecutionModel.ScriptServices;
using NetPad.Presentation;
using NetPad.Presentation.Html;
using O2Html;

namespace NetPad.Runtime.Tests.Presentation;

[CollectionDefinition(Name, DisableParallelization = true)]
public class DumpSinkCollection
{
    public const string Name = "Replaces the static dump sink";
}

[Collection(DumpSinkCollection.Name)]
public class DumpExtensionTests : IDisposable
{
    private readonly RecordingSink _sink = new();

    public DumpExtensionTests() => DumpExtension.UseSink(_sink);

    public void Dispose() => DumpExtension.UseSink(new NullDumpSink());

    [Fact]
    public void AnAsyncIteratorsItemsReachTheSinkInOrderAsAnEnumerable()
    {
        Numbers(3).Dump();

        var dumped = Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value);
        Assert.Equal(new[] { 0, 1, 2 }, dumped);
    }

    [Fact]
    public void TheTitleFormPassesItsOptionsAlong()
    {
        Numbers(2).Dump("Title", "css-class", "json", 500);

        Assert.Equal(new[] { 0, 1 }, Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value));
        Assert.Equal(new DumpOptions(Title: "Title", CssClasses: "css-class", CodeType: "json", DestructAfterMs: 500), _sink.Options);
    }

    [Fact]
    public void TheOptionsFormPassesItsOptionsAlong()
    {
        var options = new DumpOptions(Title: "Title");

        Numbers(2).Dump(options);

        Assert.Equal(new[] { 0, 1 }, Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value));
        Assert.Same(options, _sink.Options);
    }

    [Fact]
    public void UtilDumpWithATitleReadsTheSequence()
    {
        var sequence = Numbers(2);

        var returned = Util.Dump(sequence, "Title");

        Assert.Same(sequence, returned);
        Assert.Equal(new[] { 0, 1 }, Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value));
        Assert.Equal("Title", _sink.Options?.Title);
    }

    [Fact]
    public void UtilDumpWithOptionsReadsTheSequence()
    {
        var sequence = Numbers(2);
        var options = new DumpOptions(Title: "Title");

        var returned = Util.Dump(sequence, options);

        Assert.Same(sequence, returned);
        Assert.Equal(new[] { 0, 1 }, Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value));
        Assert.Same(options, _sink.Options);
    }

    [Fact]
    public void DumpReturnsTheSameSequence()
    {
        var sequence = Numbers(1);

        Assert.Same(sequence, sequence.Dump());
        Assert.Same(sequence, sequence.Dump(new DumpOptions()));
    }

    [Fact]
    public void ANullSequenceIsDumpedAsNull()
    {
        IAsyncEnumerable<int>? sequence = null;

        Assert.Null(sequence.Dump());

        Assert.True(_sink.WasWritten);
        Assert.Null(_sink.Value);
    }

    [Fact]
    public void ANullSequenceIsDumpedAsNullThroughTheOptionsForm()
    {
        IAsyncEnumerable<int>? sequence = null;

        Assert.Null(sequence.Dump(new DumpOptions()));

        Assert.True(_sink.WasWritten);
        Assert.Null(_sink.Value);
    }

    [Fact]
    public void StoppingEarlyDisposesTheSequence()
    {
        var disposed = false;

        Tracked(() => disposed = true).Dump();

        var dumped = Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value);
        Assert.Equal(new[] { 0, 1 }, dumped.Take(2));
        Assert.True(disposed);
    }

    [Fact]
    public void AnEndlessSequenceStopsAtTheCollectionLimitAndSaysThereAreMore()
    {
        var options = new HtmlSerializerOptions { MaxCollectionSerializeLength = 5 };

        Endless().Dump();
        var html = Readable(HtmlSerializer.Serialize(_sink.Value, options).ToHtml());

        Assert.Contains("First 5 items", html);
        Assert.Contains(">4<", html);
        Assert.DoesNotContain(">5<", html);
    }

    [Fact]
    public void AValueTypedAsAnAsyncSequenceUsesTheAsyncOverload()
    {
        IAsyncEnumerable<int> sequence = new SyncAndAsyncSequence();

        sequence.Dump();

        Assert.NotSame(sequence, _sink.Value);
        Assert.Equal(typeof(IEnumerable<int>), _sink.DeclaredType);
    }

    [Fact]
    public void AConcreteTypeThatIsBothSyncAndAsyncKeepsTheExistingOverload()
    {
        var sequence = new SyncAndAsyncSequence();

        sequence.Dump();

        Assert.Same(sequence, _sink.Value);
        Assert.Equal(typeof(SyncAndAsyncSequence), _sink.DeclaredType);
    }

    [Fact]
    public void AConcreteTypeThatIsBothSyncAndAsyncKeepsTheExistingOverloadInUtil()
    {
        var sequence = new SyncAndAsyncSequence();

        Util.Dump(sequence);

        Assert.Same(sequence, _sink.Value);
    }

    [Fact]
    public void AThrowingSequenceThrowsItsOwnExceptionFromTheEnumerable()
    {
        ThrowsAfterTwo().Dump();

        var dumped = Assert.IsAssignableFrom<IEnumerable<int>>(_sink.Value);
        var read = new List<int>();

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var item in dumped) read.Add(item);
        });
        Assert.Equal(new[] { 0, 1 }, read);
    }

    [Fact]
    public void DumpingASpanPassesClearAlong()
    {
        new[] { 1, 2 }.AsSpan().Dump(clear: 500);

        Assert.Equal(500, _sink.Options?.DestructAfterMs);
    }

    [Fact]
    public void DumpingAReadOnlySpanPassesClearAlong()
    {
        ((ReadOnlySpan<int>)new[] { 1, 2 }).Dump(clear: 500);

        Assert.Equal(500, _sink.Options?.DestructAfterMs);
    }

    [Fact]
    public void AThrowingSequenceIsPresentedLikeALazyEnumerableThatThrows()
    {
        ThrowsAfterTwo().Dump();
        var asyncHtml = Readable(HtmlPresenter.Serialize(_sink.Value));

        LazyThrowsAfterTwo().Dump();
        var lazyHtml = Readable(HtmlPresenter.Serialize(_sink.Value));

        Assert.Contains("Could not serialize object to HTML", asyncHtml);
        Assert.Contains("Could not serialize object to HTML", lazyHtml);
        Assert.Contains("Part-way failure", asyncHtml);
        Assert.Contains("Part-way failure", lazyHtml);
        Assert.DoesNotContain("<td", asyncHtml);
        Assert.DoesNotContain("<td", lazyHtml);
    }

    private static string Readable(string html) => html.Replace("&nbsp;", " ");

    private static async IAsyncEnumerable<int> Numbers(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    private static async IAsyncEnumerable<int> Endless()
    {
        for (var i = 0; ; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    private static async IAsyncEnumerable<int> Tracked(Action onDisposed)
    {
        try
        {
            for (var i = 0; ; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }
        finally
        {
            onDisposed();
        }
    }

    private static async IAsyncEnumerable<int> ThrowsAfterTwo()
    {
        await Task.Yield();
        yield return 0;
        yield return 1;
        throw new InvalidOperationException("Part-way failure");
    }

    private static IEnumerable<int> LazyThrowsAfterTwo()
    {
        yield return 0;
        yield return 1;
        throw new InvalidOperationException("Part-way failure");
    }

    private sealed class RecordingSink : IDumpSink
    {
        public object? Value { get; private set; }
        public DumpOptions? Options { get; private set; }
        public Type? DeclaredType { get; private set; }
        public bool WasWritten { get; private set; }

        public void ResultWrite<T>(T? o, DumpOptions? options = null)
        {
            Value = o;
            Options = options;
            DeclaredType = typeof(T);
            WasWritten = true;
        }

        public void SqlWrite<T>(T? o, DumpOptions? options = null)
        {
        }
    }

    private sealed class SyncAndAsyncSequence : IEnumerable<int>, IAsyncEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() => Enumerable.Range(0, 2).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            Numbers(2).GetAsyncEnumerator(cancellationToken);
    }
}
