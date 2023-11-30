using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Presentation.Html;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.UiInterop;
using O2Html;
using Timer = System.Timers.Timer;

namespace NetPad.Services;

/// <summary>
/// An <see cref="IOutputWriter{TOutput}"/> that coordinates sending of script output messages emitted by ScriptEnvironments to IPC clients.
/// It employs queueing and max output limits to prevent over-flooding IPC with too much data.
/// </summary>
public sealed record ScriptEnvironmentIpcOutputWriter : IOutputWriter<object>, IDisposable
{
    private readonly ScriptEnvironment _scriptEnvironment;
    private readonly IIpcService _ipcService;
    private readonly List<IDisposable> _disposables;
    private readonly ILogger<ScriptEnvironmentIpcOutputWriter> _logger;

    private readonly Accessor<CancellationTokenSource> _ctsAccessor;
    private readonly ConcurrentQueue<IpcMessage> _sendMessageQueue;
    private readonly Timer _sendMessageQueueTimer;
    private const int _sendMessageQueueBatchSize = 1000;
    private const int _processSendMessageQueueEveryMs = 50;
    private const int _maxUserOutputMessagesPerRun = 10100;
    private int _userOutputMessagesSentThisRun;
    private bool _outputLimitReachedMessageSent;
    private readonly object _outputLimitReachedMessageSendLock = new();

    public ScriptEnvironmentIpcOutputWriter(
        ScriptEnvironment scriptEnvironment,
        IIpcService ipcService,
        IEventBus eventBus,
        ILogger<ScriptEnvironmentIpcOutputWriter> logger)
    {
        _scriptEnvironment = scriptEnvironment;
        _ipcService = ipcService;
        _logger = logger;
        _disposables = new List<IDisposable>();

        _ctsAccessor = new Accessor<CancellationTokenSource>(new CancellationTokenSource());
        _sendMessageQueue = new();

        _sendMessageQueueTimer = new Timer()
        {
            Interval = _processSendMessageQueueEveryMs,
            AutoReset = false,
            Enabled = false
        };

        _sendMessageQueueTimer.Elapsed += async (_, _) => await ProcessSendMessageQueue(_sendMessageQueueBatchSize);

        _disposables.Add(eventBus.Subscribe<EnvironmentPropertyChangedEvent>(msg =>
        {
            if (msg.ScriptId == _scriptEnvironment.Script.Id && msg.PropertyName == nameof(ScriptEnvironment.Status))
            {
                NotifyScriptEnvironmentStatusChanged((ScriptStatus)msg.NewValue!);
            }

            return Task.CompletedTask;
        }));
    }

    private void NotifyScriptEnvironmentStatusChanged(ScriptStatus newStatus)
    {
        if (newStatus == ScriptStatus.Running)
        {
            // Starting a new run
            _sendMessageQueueTimer.Stop();

            _ctsAccessor.Value.Cancel();
            _sendMessageQueue.Clear();
            _userOutputMessagesSentThisRun = 0;
            _outputLimitReachedMessageSent = false;
            _ctsAccessor.Update(new CancellationTokenSource());

            _sendMessageQueueTimer.Start();
        }
        else if (newStatus == ScriptStatus.Stopping)
        {
            // When a script is stopped explicitly, cancel sending all cancellable output messages
            _ctsAccessor.Value.Cancel();
        }
    }


    /// <summary>
    /// Decides which output types should be queued vs sent right-away.
    /// When a message is queued, this method will return a completed task. The queued message might not go out till later.
    /// </summary>
    public async Task WriteAsync(object? output, string? title = null, CancellationToken cancellationToken = default)
    {
        // Since we want the end result to be HTML-encoded, any output that is not an HtmlScriptOutput will be converted to
        // its corresponding HtmlScriptOutput type before pushing to IPC clients.

        if (output is HtmlResultsScriptOutput htmlResultsScriptOutput)
        {
            if (HasReachedUserOutputMessageLimitForThisRun())
            {
                if (_outputLimitReachedMessageSent) return;

                lock (_outputLimitReachedMessageSendLock)
                {
                    if (_outputLimitReachedMessageSent) return;

                    var message = new HtmlRawScriptOutput(HtmlPresenter.SerializeToElement(
                            "Output limit reached.",
                            new DumpOptions(AppendNewLine: true))
                        .WithAddClass("raw")
                        .ToHtml()
                    );
                    QueueMessage(message, true);
                    _outputLimitReachedMessageSent = true;
                }

                return;
            }

            Interlocked.Increment(ref _userOutputMessagesSentThisRun);

            QueueMessage(htmlResultsScriptOutput, true);
        }
        else if (output is SqlScriptOutput sqlScriptOutput)
        {
            var htmlSqlScriptOutput = new HtmlSqlScriptOutput(
                sqlScriptOutput.Order,
                HtmlPresenter.Serialize(sqlScriptOutput.Body, new DumpOptions(Title: title)));

            await PushToIpcAsync(new ScriptOutputEmittedEvent(_scriptEnvironment.Script.Id, htmlSqlScriptOutput), _ctsAccessor.Value.Token);
        }
        else if (output is HtmlSqlScriptOutput htmlSqlScriptOutput)
        {
            await PushToIpcAsync(new ScriptOutputEmittedEvent(_scriptEnvironment.Script.Id, htmlSqlScriptOutput), _ctsAccessor.Value.Token);
        }
        else if (output is RawScriptOutput rawScriptOutput)
        {
            var htmlRawScriptOutput = new HtmlRawScriptOutput(
                rawScriptOutput.Order,
                HtmlPresenter.SerializeToElement(
                        rawScriptOutput.Body,
                        new DumpOptions(Title: title, AppendNewLine: true)
                    )
                    .WithAddClass("raw")
                    .ToHtml()
            );

            QueueMessage(htmlRawScriptOutput, false);
        }
        else if (output is HtmlRawScriptOutput htmlRawScriptOutput)
        {
            QueueMessage(htmlRawScriptOutput, false);
        }
        else if (output is ErrorScriptOutput errorScriptOutput)
        {
            var htmlErrorOutput = new HtmlErrorScriptOutput(
                errorScriptOutput.Order,
                HtmlPresenter.Serialize(
                    errorScriptOutput.Body,
                    new DumpOptions(Title: title, AppendNewLine: true),
                    isError: true));

            QueueMessage(htmlErrorOutput, false);
        }
        else if (output is HtmlErrorScriptOutput htmlErrorScriptOutput)
        {
            QueueMessage(htmlErrorScriptOutput, false);
        }
        else if (output is ScriptOutput scriptOutput)
        {
            var htmlRawOutput = new HtmlRawScriptOutput(
                scriptOutput.Order,
                HtmlPresenter.Serialize(output, new DumpOptions(Title: title)));

            QueueMessage(htmlRawOutput, true);
        }
        else
        {
            _logger.LogWarning("Unexpected script output format: {OutputType}", output?.GetType().Name);

            var htmlRawOutput = new HtmlRawScriptOutput(0, HtmlPresenter.Serialize(output, new DumpOptions(Title: title)));

            QueueMessage(htmlRawOutput, true);
        }
    }

    private bool HasReachedUserOutputMessageLimitForThisRun()
    {
        return _userOutputMessagesSentThisRun >= _maxUserOutputMessagesPerRun;
    }

    private void QueueMessage(ScriptOutput output, bool isCancellable)
    {
        var cancellationToken = isCancellable ? _ctsAccessor.Value.Token : CancellationToken.None;

        if (isCancellable && cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var message = new IpcMessage(new ScriptOutputEmittedEvent(_scriptEnvironment.Script.Id, output), cancellationToken);

        _sendMessageQueue.Enqueue(message);
    }

    private async Task ProcessSendMessageQueue(int maxMessagesToSend)
    {
        try
        {
            if (!_sendMessageQueue.Any()) return;

            var messages = new List<IpcMessage>();

            while (messages.Count < maxMessagesToSend && _sendMessageQueue.TryDequeue(out var queuedMessage))
            {
                if (queuedMessage.CancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                messages.Add(queuedMessage);
            }

            if (!messages.Any()) return;

            await PushToIpcAsync(new IpcMessageBatch(messages), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending messages from queue to clients");
        }
        finally
        {
            _sendMessageQueueTimer.Start();
        }
    }

    private async Task PushToIpcAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        await _ipcService.SendAsync(message, cancellationToken);
    }

    public void Dispose()
    {
        _ctsAccessor.Value.Cancel();
        _sendMessageQueueTimer.Stop();
        _sendMessageQueueTimer.Dispose();
        _sendMessageQueue.Clear();

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
