using System.Collections.Concurrent;
using NetPad.IO;
using NetPad.Presentation;

namespace NetPad.ExecutionModel;

/// <summary>
/// Handles unstructured raw process output. Used to buffer output and give it order. Usually raw process output
/// is not a result of user output and can be unpredictable (ex. stackoverflow output can be thousands of lines in rapid succession).
///
/// Another use for this is that raw process output comes in line-by-line. An error emitted as a result of an uncaught
/// exception for example will span multiple lines, each line will come in one by one. The debounced queue
/// implemented here will ensure that those lines will be bundled and written to output as one message.
/// </summary>
internal class RawOutputHandler
{
    private uint _rawOutputOrder;
    private uint _rawOutputPushOrder;
    private uint _rawErrorOrder;
    private uint _rawErrorPushOrder;
    private readonly ConcurrentQueue<(uint order, string output)> _rawOutputs;
    private readonly ConcurrentQueue<(uint order, string output)> _rawErrors;
    private readonly IOutputWriter<object> _scriptOutputWriter;
    private readonly Action PushOutputs;
    private readonly Action PushErrors;

    public RawOutputHandler(IOutputWriter<object> scriptOutputWriter)
    {
        _scriptOutputWriter = scriptOutputWriter;
        _rawOutputs = new();
        _rawErrors = new();

        PushOutputs = new Func<Task>(async () =>
        {
            var list = new List<(uint order, string output)>();

            while (_rawOutputs.TryDequeue(out var output))
                list.Add(output);

            if (!list.Any()) return;

            string finalOutput = list.OrderBy(x => x.order).Select(x => x.output).JoinToString("\n") + "\n";

            await scriptOutputWriter.WriteAsync(new ScriptOutput(
                ScriptOutputKind.Result,
                Interlocked.Increment(ref _rawOutputPushOrder) - 1,
                finalOutput));
        }).DebounceAsync();

        PushErrors = new Func<Task>(async () =>
        {
            var list = new List<(uint order, string output)>();

            while (_rawErrors.TryDequeue(out var output))
                list.Add(output);

            if (!list.Any()) return;

            string finalOutput = list.OrderBy(x => x.order).Select(x => x.output).JoinToString("\n") + "\n";

            await scriptOutputWriter.WriteAsync(new ScriptOutput(
                ScriptOutputKind.Error,
                Interlocked.Increment(ref _rawErrorPushOrder) - 1,
                finalOutput));
        }).DebounceAsync();
    }

    public void Reset()
    {
        _rawOutputOrder = 0;
        _rawOutputPushOrder = 0;
        _rawErrorOrder = 0;
        _rawErrorPushOrder = 0;
    }

    public void RawOutputReceived(string output)
    {
        _rawOutputs.Enqueue((Interlocked.Increment(ref _rawOutputOrder) - 1, output));
        PushOutputs();
    }

    public void RawErrorReceived(string output)
    {
        _rawErrors.Enqueue((Interlocked.Increment(ref _rawErrorOrder) - 1, output));
        PushErrors();
    }

    /// <summary>
    /// Immediately flushes any buffered output and error messages, bypassing the debounce delay.
    /// Call after the external process has exited to ensure all output is delivered before
    /// returning results.
    /// </summary>
    public void Flush()
    {
        FlushQueue(_rawOutputs, ref _rawOutputPushOrder, isError: false);
        FlushQueue(_rawErrors, ref _rawErrorPushOrder, isError: true);
    }

    private void FlushQueue(ConcurrentQueue<(uint order, string output)> queue, ref uint pushOrder, bool isError)
    {
        var list = new List<(uint order, string output)>();

        while (queue.TryDequeue(out var item))
            list.Add(item);

        if (list.Count == 0) return;

        string finalOutput = list.OrderBy(x => x.order).Select(x => x.output).JoinToString("\n") + "\n";

        ScriptOutput scriptOutput = new ScriptOutput(
            isError ? ScriptOutputKind.Error : ScriptOutputKind.Result,
            Interlocked.Increment(ref pushOrder) - 1,
            finalOutput);

        _scriptOutputWriter.WriteAsync(scriptOutput).GetAwaiter().GetResult();
    }
}
