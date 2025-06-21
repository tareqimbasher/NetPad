using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Presentation.Html;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using O2Html;
using Timer = System.Timers.Timer;

namespace NetPad.Services;

/// <summary>
/// An <see cref="IOutputWriter{TOutput}"/> that coordinates sending of script output messages emitted by
/// ScriptEnvironments to IPC clients, mainly the front end SPA. It employs queueing and max output limits
/// to prevent over-flooding IPC channel with too much data.
/// </summary>
public sealed record ScriptEnvironmentIpcOutputWriter : IOutputWriter<object>, IDisposable
{
    private const int SendMessageQueueBatchSize = 1000;
    private const int ProcessSendMessageQueueEveryMs = 50;
    private const int MaxUserOutputMessagesPerRun = 10100;

    private readonly ScriptEnvironment _scriptEnvironment;
    private readonly IIpcService _ipcService;
    private readonly List<IDisposable> _disposables;
    private readonly ILogger<ScriptEnvironmentIpcOutputWriter> _logger;

    private readonly Accessor<CancellationTokenSource> _ctsAccessor;
    private readonly ConcurrentQueue<IpcMessage> _sendMessageQueue;
    private readonly Timer _sendMessageQueueTimer;
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
        _disposables = [];

        _ctsAccessor = new Accessor<CancellationTokenSource>(new CancellationTokenSource());
        _sendMessageQueue = new();

        _sendMessageQueueTimer = new Timer()
        {
            Interval = ProcessSendMessageQueueEveryMs,
            AutoReset = false,
            Enabled = false
        };

        _sendMessageQueueTimer.Elapsed += async (_, _) => await ProcessSendMessageQueue(SendMessageQueueBatchSize);

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
        try
        {
            if (newStatus == ScriptStatus.Running)
            {
                // Starting a new run
                _sendMessageQueueTimer.Stop();

                _ctsAccessor.Value.Cancel();
                _ctsAccessor.Value.Dispose();
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
                _ctsAccessor.Value.Dispose();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error handling script status change");
        }
    }


    /// <summary>
    /// Decides which output types should be queued vs sent right-away.
    /// When a message is queued, this method will return a completed task. The queued message might not go out till later.
    /// </summary>
    public async Task WriteAsync(object? output, string? title = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

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
                            new DumpOptions(AppendNewLineToAllTextOutput: true))
                        .AddClass("raw")
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
                        new DumpOptions(Title: title, AppendNewLineToAllTextOutput: true)
                    )
                    .AddClass("raw")
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
                    new DumpOptions(Title: title, AppendNewLineToAllTextOutput: true),
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
        return _userOutputMessagesSentThisRun >= MaxUserOutputMessagesPerRun;
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
        Try.Run(() =>
        {
            _ctsAccessor.Value.Cancel();
            _ctsAccessor.Value.Dispose();
        });
        _sendMessageQueueTimer.Stop();
        _sendMessageQueueTimer.Dispose();
        _sendMessageQueue.Clear();

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
