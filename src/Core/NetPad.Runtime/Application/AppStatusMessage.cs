namespace NetPad.Application;

/// <summary>
/// Represents a status change in the application.
/// </summary>
public class AppStatusMessage(
    string text,
    AppStatusMessagePriority priority = AppStatusMessagePriority.Normal,
    bool persistant = false)
{
    public AppStatusMessage(Guid scriptId, string text, AppStatusMessagePriority priority = AppStatusMessagePriority.Normal, bool persistant = false)
        : this(text, priority, persistant)
    {
        ScriptId = scriptId;
    }

    /// <summary>
    /// The ID of the script this message relates to.
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
    /// Whether this status message should be persistant or it should clear out after a timeout.
    /// </summary>
    public bool Persistant { get; } = persistant;

    /// <summary>
    /// The DateTime of when this message was created.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;
}

public enum AppStatusMessagePriority
{
    Normal = 1,
    High = 2
}
