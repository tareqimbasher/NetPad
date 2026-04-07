using System.Collections.Concurrent;
using System.Collections.Generic;
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
/// to prevent over-flooding IPC channel with too many messages.
/// </summary>
public sealed record ScriptEnvironmentIpcOutputWriter : IOutputWriter<object>, IDisposable
{
    /// <summary>The number of messages to send in a single batch.</summary>
    private const int SendMessageQueueBatchSize = 1000;

    /// <summary>How often to process the message queue.</summary>
    private const int ProcessSendMessageQueueEveryMs = 50;

    /// <summary>
    /// The max number of messages that is allowed to be outputted by a single script run/execution.
    /// Any messages produced after this number will not be sent to IPC clients.
    /// </summary>
    private const int MaxUserOutputMessagesPerRun = 10100;

    private readonly ScriptEnvironment _scriptEnvironment;
    private readonly IIpcService _ipcService;
    private readonly List<IDisposable> _disposables = [];
    private readonly ILogger<ScriptEnvironmentIpcOutputWriter> _logger;

    private readonly Accessor<CancellationTokenSource> _ctsAccessor = new(new CancellationTokenSource());
    private readonly ConcurrentQueue<IpcMessage> _sendMessageQueue = new();
    private readonly Timer _sendMessageQueueTimer;
    private int _userOutputMessagesSentThisRun;
    private bool _sentOutputLimitReachedMessage;
    private readonly Lock _sendOutputLimitReachedMessageLock = new();

    public ScriptEnvironmentIpcOutputWriter(
        ScriptEnvironment scriptEnvironment,
        IIpcService ipcService,
        IEventBus eventBus,
        ILogger<ScriptEnvironmentIpcOutputWriter> logger)
    {
        _scriptEnvironment = scriptEnvironment;
        _ipcService = ipcService;
        _logger = logger;

        _sendMessageQueueTimer = new Timer
        {
            Interval = ProcessSendMessageQueueEveryMs,
            AutoReset = false,
            Enabled = false
        };

        _sendMessageQueueTimer.Elapsed += (_, _) => ProcessSendMessageQueue();

        _disposables.Add(eventBus.Subscribe<EnvironmentPropertyChangedEvent>(msg =>
        {
            if (msg.ScriptId == _scriptEnvironment.Script.Id && msg.PropertyName == nameof(ScriptEnvironment.Status))
            {
                OnScriptEnvironmentStatusChanged((ScriptStatus)msg.NewValue!);
            }

            return Task.CompletedTask;
        }));
    }

    private void OnScriptEnvironmentStatusChanged(ScriptStatus newStatus)
    {
        try
        {
            if (newStatus == ScriptStatus.Running)
            {
                // Starting a new run
                _sendMessageQueueTimer.Stop();

                // Cancel the old CTS, don't dispose it till further down
                var old = _ctsAccessor.Value;
                Try.Run(old.Cancel);

                _sendMessageQueue.Clear();
                _userOutputMessagesSentThisRun = 0;
                _sentOutputLimitReachedMessage = false;

                // Swap in a new CTS for this run then dispose old one
                _ctsAccessor.Update(new CancellationTokenSource());
                Try.Run(old.Dispose);

                _sendMessageQueueTimer.Start();
            }
            else if (newStatus == ScriptStatus.Stopping)
            {
                // When a script is stopped explicitly, cancel sending all cancellable output messages
                Try.Run(_ctsAccessor.Value.Cancel);
                Try.Run(_ctsAccessor.Value.Dispose);
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

        if (output is not ScriptOutput so)
        {
            return;
        }

        // Ensure output is HTML-formatted for the frontend
        if (so.Format != ScriptOutputFormat.Html)
        {
            so = so.Kind switch
            {
                ScriptOutputKind.Error => so with
                {
                    Body = HtmlPresenter.Serialize(so.Body, new DumpOptions(Title: title, AppendNewLineToAllTextOutput: true), isError: true),
                    Format = ScriptOutputFormat.Html
                },
                ScriptOutputKind.Sql => so with
                {
                    Body = HtmlPresenter.Serialize(so.Body, new DumpOptions(Title: title)),
                    Format = ScriptOutputFormat.Html
                },
                _ => so with
                {
                    Body = HtmlPresenter.SerializeToElement(so.Body, new DumpOptions(Title: title, AppendNewLineToAllTextOutput: true))
                        .AddClass("raw").ToHtml(),
                    Format = ScriptOutputFormat.Html
                }
            };
        }

        // Route based on kind
        switch (so.Kind)
        {
            case ScriptOutputKind.Result:
                if (HasReachedUserOutputMessageLimitForThisRun())
                {
                    if (_sentOutputLimitReachedMessage) return;

                    lock (_sendOutputLimitReachedMessageLock)
                    {
                        if (_sentOutputLimitReachedMessage) return;

                        var message = new ScriptOutput(
                            ScriptOutputKind.Result, 0,
                            HtmlPresenter.SerializeToElement("Output limit reached.", new DumpOptions(AppendNewLineToAllTextOutput: true))
                                .AddClass("raw").ToHtml(),
                            ScriptOutputFormat.Html);
                        QueueMessage(message, true);
                        _sentOutputLimitReachedMessage = true;
                    }

                    return;
                }

                Interlocked.Increment(ref _userOutputMessagesSentThisRun);
                QueueMessage(so, true);
                break;

            case ScriptOutputKind.Sql:
                await PushToIpcAsync(new ScriptOutputEmittedEvent(_scriptEnvironment.Script.Id, so),
                    _ctsAccessor.Value.Token);
                break;

            case ScriptOutputKind.Error:
                QueueMessage(so, false);
                break;
        }
    }

    private bool HasReachedUserOutputMessageLimitForThisRun()
    {
        return _userOutputMessagesSentThisRun >= MaxUserOutputMessagesPerRun;
    }

    private void QueueMessage(ScriptOutput output, bool isCancellable)
    {
        var cancellationToken = CancellationToken.None;

        if (isCancellable)
        {
            try
            {
                cancellationToken = _ctsAccessor.Value.Token;
            }
            catch (ObjectDisposedException)
            {
                // CTS was disposed or swapped out; treat as canceled and drop the message
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }

        var message = new IpcMessage(
            new ScriptOutputEmittedEvent(_scriptEnvironment.Script.Id, output),
            cancellationToken);

        _sendMessageQueue.Enqueue(message);
    }

    private async void ProcessSendMessageQueue()
    {
        try
        {
            if (_sendMessageQueue.IsEmpty) return;

            var messages = new List<IpcMessage>();

            while (messages.Count <= SendMessageQueueBatchSize && _sendMessageQueue.TryDequeue(out var queuedMessage))
            {
                if (queuedMessage.CancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                messages.Add(queuedMessage);
            }

            if (messages.Count == 0) return;

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
            Try.Run(_ctsAccessor.Value.Cancel);
            Try.Run(_ctsAccessor.Value.Dispose);
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
