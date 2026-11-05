using Microsoft.Extensions.Logging;
using NetPad.Packages.NuGet;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NetPad.Runtime.Tests.Packages.NuGet;

public class PackageSourceCallerTests
{
    private readonly PackageSourceHealth _health = new();
    private readonly CountingLogger _logger = new();
    private readonly PackageSourceCaller _caller;
    private readonly PackageSource _source = new("https://example.test/feed/index.json", "Example");

    public PackageSourceCallerTests()
    {
        _caller = new PackageSourceCaller(_health, _logger);
    }

    // NuGet reports a cancelled HTTP request this way, not as an OperationCanceledException.
    private static FatalProtocolException WrappedCancellation() =>
        new("Failed to retrieve information about 'Some.Package'", new TaskCanceledException());

    [Fact]
    public async Task A_Success_Returns_The_Value_And_Reinstates_The_Source()
    {
        _health.RecordFailure(_source);

        var (succeeded, value) = await _caller.CallAsync(_source, "test", () => Task.FromResult(42), CancellationToken.None);

        Assert.True(succeeded);
        Assert.Equal(42, value);
        Assert.False(_health.ShouldSkip(_source));
    }

    [Fact]
    public async Task A_Wrapped_Cancellation_With_The_Token_Cancelled_Leaves_The_Source_Healthy()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _caller.CallAsync<int>(_source, "test", () => throw WrappedCancellation(), cts.Token));

        Assert.False(_health.ShouldSkip(_source));
        Assert.Equal(0, _logger.Warnings);
    }

    [Fact]
    public async Task The_Same_Exception_Without_A_Cancelled_Token_Counts_Against_The_Source()
    {
        var (succeeded, _) = await _caller.CallAsync<int>(_source, "test", () => throw WrappedCancellation(), CancellationToken.None);

        Assert.False(succeeded);
        Assert.True(_health.ShouldSkip(_source));
        Assert.Equal(1, _logger.Warnings);
    }

    [Fact]
    public async Task Parallel_Failures_In_One_Episode_Log_One_Warning()
    {
        using var start = new ManualResetEventSlim();

        var calls = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => _caller.CallAsync<int>(_source, "test", () =>
            {
                start.Wait();
                throw new HttpRequestException("unreachable");
            }, CancellationToken.None)))
            .ToArray();

        start.Set();
        await Task.WhenAll(calls);

        Assert.Equal(1, _logger.Warnings);
    }

    private sealed class CountingLogger : ILogger
    {
        private int _warnings;

        public int Warnings => _warnings;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Interlocked.Increment(ref _warnings);
            }
        }
    }
}
