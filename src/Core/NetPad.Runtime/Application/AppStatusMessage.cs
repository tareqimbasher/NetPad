namespace NetPad.Application;

/// <summary>
/// A message that represents a status change in the application meant to be shown to users on the UI.
/// </summary>
public class AppStatusMessage(
    string text,
    AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
    bool persistent = false)
{
    public AppStatusMessage(
        Guid scriptId,
        string text,
        AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
        bool persistent = false
    ) : this(text, priority, persistent)
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
    /// Whether this status message should be persistent or if it should be cleared after a timeout.
    /// </summary>
    public bool Persistent { get; } = persistent;

    /// <summary>
    /// The date and time when this message was created.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;
}
