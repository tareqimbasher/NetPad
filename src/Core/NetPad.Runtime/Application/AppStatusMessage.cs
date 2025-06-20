namespace NetPad.Application;

/// <summary>
/// Represents a status change in the application meant to be shown on UIs for users.
/// </summary>
public class AppStatusMessage(
    string text,
    AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
    bool persistant = false)
{
    public AppStatusMessage(
        Guid scriptId,
        string text,
        AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
        bool persistant = false
    ) : this(text, priority, persistant)
    {
        ScriptId = scriptId;
    }

    /// <summary>
    /// The ID of the script this message relates to, if any.
    /// </summary>
    public Guid? ScriptId { get; }

    /// <summary>
    /// The text of this message.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// The priority of this message.
    /// </summary>
    public AppStatusMessagePriority Priority { get; } = priority;

    /// <summary>
    /// Whether this status message should be persistant or if it should be cleared after a timeout.
    /// </summary>
    public bool Persistant { get; } = persistant;

    /// <summary>
    /// The date and time when this message was created.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;
}
