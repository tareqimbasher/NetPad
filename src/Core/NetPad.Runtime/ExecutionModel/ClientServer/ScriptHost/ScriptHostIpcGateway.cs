using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.IO.IPC.Stdio;

namespace NetPad.ExecutionModel.ClientServer.ScriptHost;

/// <summary>
/// A specialized <see cref="StdioIpcGateway{TMessage}"/> to interface with script-host process.
/// </summary>
/// <param name="sendChannel">The STD Input of the script-host process.</param>
/// <param name="logger">An optional logger.</param>
public sealed class ScriptHostIpcGateway(TextWriter sendChannel, ILogger? logger = null)
    : StdioIpcGateway<ScriptHostIpcMessage>(sendChannel)
{
    private readonly Dictionary<Type, IpcMessageHandler> _ipcMessageHandlers = new();

    /// <summary>
    /// Starts listening to messages sent by script-host process.
    /// </summary>
    /// <param name="receiveChannel">The STD Output of the script-host process.</param>
    /// <param name="onNonMessageReceived">A handler to process script-host output that cannot be deserialized to a <see cref="ScriptHostIpcMessage"/>.</param>
    public void Listen(
        TextReader receiveChannel,
        Action<string>? onNonMessageReceived = null)
    {
        base.Listen(
            receiveChannel,
            OnMessageReceivedFromScriptHost,
            onNonMessageReceived);
    }

    /// <summary>
    /// Adds a handler to execute when a message of the specified type is received.
    /// </summary>
    /// <param name="action">The message handler.</param>
    /// <typeparam name="TMessage">The type of message to listen for.</typeparam>
    public void On<TMessage>(Action<TMessage> action) where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (!_ipcMessageHandlers.TryGetValue(messageType, out var handler))
        {
            handler = new IpcMessageHandler<TMessage>(logger);
            _ipcMessageHandlers[messageType] = handler;
        }

        ((IpcMessageHandler<TMessage>)handler).Actions.Add(action);
    }

    private void OnMessageReceivedFromScriptHost(ScriptHostIpcMessage ipcMessage)
    {
        var ipcMsgType = Type.GetType(ipcMessage.Type);

        if (ipcMsgType == null)
        {
            logger?.LogError("Unknown IPC message type: {Type}", ipcMessage.Type);
            return;
        }

        if (_ipcMessageHandlers.TryGetValue(ipcMsgType, out var handler))
        {
            handler.Handle(ipcMessage);
        }
        else
        {
            logger?.LogWarning("No handler registered for message type: {IpcMsgType}", ipcMsgType.FullName);
        }
    }

    /// <summary>
    /// Executes any handlers associated with the specified message type.
    /// </summary>
    public void ExecuteHandlers<TMessage>(TMessage message) where TMessage : class
    {
        if (_ipcMessageHandlers.TryGetValue(typeof(TMessage), out var handler))
        {
            ((IpcMessageHandler<TMessage>)handler).Handle(message);
        }
    }

    /// <summary>
    /// Sends a message over IPC.
    /// </summary>
    /// <param name="seq">The sequence (order) of this message. This can be useful to the receiver if it wants
    /// to process messages in order.</param>
    /// <param name="message">The message to send.</param>
    public void Send<TMessage>(uint seq, TMessage message)
    {
        var messageType = typeof(TMessage).FullName;
        if (messageType == null)
        {
            throw new ArgumentException("Message type fullname is null");
        }

        base.Send(new ScriptHostIpcMessage(seq, messageType, JsonSerializer.Serialize(message)));
    }
}

internal abstract class IpcMessageHandler
{
    public abstract void Handle(ScriptHostIpcMessage ipcMessage);
}

internal class IpcMessageHandler<TMessage>(ILogger? logger = null) : IpcMessageHandler where TMessage : class
{
    public readonly List<Action<TMessage>> Actions = [];

    public override void Handle(ScriptHostIpcMessage ipcMessage)
    {
        TMessage? message;

        try
        {
            message = JsonSerializer.Deserialize<TMessage>(ipcMessage.Data);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to deserialize IPC message to type: {MessageType}. Will not execute handlers",
                ipcMessage.Type);
            return;
        }

        if (message == null)
        {
            logger?.LogError("Deserialized IPC message of type: {MessageType} to null. Will not execute handlers",
                ipcMessage.Type);
            return;
        }

        Handle(message);
    }

    public void Handle(TMessage message)
    {
        logger?.LogDebug("Received message: {MessageType}", typeof(TMessage).Name);

        foreach (var handler in Actions)
        {
            try
            {
                handler(message);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error executing IPC message handler for type: {MessageType}",
                    message.GetType().FullName);
            }
        }
    }
}
