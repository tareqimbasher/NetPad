using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class StreamUtilTests
{
    [Fact]
    public async Task CopyToAsync_CopiesAllData()
    {
        var sourceData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var source = new MemoryStream(sourceData);
        using var destination = new MemoryStream();

        await source.CopyToAsync(destination, bufferSize: 4);

        Assert.Equal(sourceData, destination.ToArray());
    }

    [Fact]
    public async Task CopyToAsync_ReportsProgress()
    {
        var sourceData = new byte[100];
        Array.Fill(sourceData, (byte)0xAB);
        using var source = new MemoryStream(sourceData);
        using var destination = new MemoryStream();

        var progressReports = new List<long>();
        var progress = new SynchronousProgress<long>(value => progressReports.Add(value));

        await source.CopyToAsync(destination, bufferSize: 30, progress);

        Assert.NotEmpty(progressReports);
        Assert.Equal(100, progressReports[^1]);
    }

    /// <summary>
    /// Unlike <see cref="Progress{T}"/>, invokes the callback synchronously
    /// so tests don't need to wait for SynchronizationContext posting.
    /// </summary>
    private class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    [Fact]
    public async Task CopyToAsync_HandlesEmptySource()
    {
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        await source.CopyToAsync(destination, bufferSize: 1024);

        Assert.Empty(destination.ToArray());
    }

    [Fact]
    public async Task CopyToAsync_ThrowsArgumentNullException_ForNullSource()
    {
        using var destination = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            StreamUtil.CopyToAsync(null!, destination, 1024));
    }

    [Fact]
    public async Task CopyToAsync_ThrowsArgumentNullException_ForNullDestination()
    {
        using var source = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.CopyToAsync(null!, 1024));
    }

    [Fact]
    public async Task CopyToAsync_ThrowsArgumentOutOfRangeException_ForNegativeBufferSize()
    {
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            source.CopyToAsync(destination, bufferSize: -1));
    }

    [Fact]
    public async Task CopyToAsync_SupportsCancellation()
    {
        var sourceData = new byte[10000];
        using var source = new MemoryStream(sourceData);
        using var destination = new MemoryStream();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            source.CopyToAsync(destination, bufferSize: 1, cancellationToken: cts.Token));
    }
}
