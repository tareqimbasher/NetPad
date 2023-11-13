using System.Collections.Concurrent;
using NetPad.IO;
using NetPad.Utilities;

namespace NetPad.Runtimes;

/// <summary>
/// Handles unstructured raw process output. Used to buffer output and give it order. Usually raw process output
/// is not user-designated output and can be unpredictable (ex. stackoverflow output can be thousands of lines in rapid succession).
/// </summary>
internal class RawOutputHandler
{
    private uint _rawResultOutputOrder;
    private uint _rawResultOutputPushOrder;
    private uint _rawErrorOutputOrder;
    private uint _rawErrorOutputPushOrder;
    private readonly ConcurrentQueue<(uint order, string output)> _rawResultOutput;
    private readonly ConcurrentQueue<(uint order, string output)> _rawErrorOutput;
    private readonly Action PushResultOutput;
    private readonly Action PushErrorOutput;

    public RawOutputHandler(IOutputWriter<object> scriptOutputAdapter)
    {
        _rawResultOutput = new();
        _rawErrorOutput = new();

        PushResultOutput = new Func<Task>(async () =>
        {
            var list = new List<(uint order, string output)>();

            while (_rawResultOutput.TryDequeue(out var output))
                list.Add(output);

            if (!list.Any()) return;

            string finalOutput = list.OrderBy(x => x.order).Select(x => x.output).JoinToString("\n") + "\n";

            await scriptOutputAdapter.WriteAsync(new RawScriptOutput(
                Interlocked.Increment(ref _rawResultOutputPushOrder) - 1,
                finalOutput));
        }).DebounceAsync();

        PushErrorOutput = new Func<Task>(async () =>
        {
            var list = new List<(uint order, string output)>();

            while (_rawErrorOutput.TryDequeue(out var output))
                list.Add(output);

            if (!list.Any()) return;

            string finalOutput = list.OrderBy(x => x.order).Select(x => x.output).JoinToString("\n") + "\n";

            await scriptOutputAdapter.WriteAsync(new ErrorScriptOutput(
                Interlocked.Increment(ref _rawErrorOutputPushOrder) - 1,
                finalOutput));
        }).DebounceAsync();
    }

    public void Reset()
    {
        _rawResultOutputOrder = 0;
        _rawResultOutputPushOrder = 0;
        _rawErrorOutputOrder = 0;
        _rawErrorOutputPushOrder = 0;
    }

    public void RawResultOutputReceived(string output)
    {
        _rawResultOutput.Enqueue((Interlocked.Increment(ref _rawResultOutputOrder) - 1, output));
        PushResultOutput();
    }

    public void RawErrorOutputReceived(string output)
    {
        _rawErrorOutput.Enqueue((Interlocked.Increment(ref _rawErrorOutputOrder) - 1, output));
        PushErrorOutput();
    }
}
