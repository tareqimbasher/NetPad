namespace NetPad.Application;

/// <summary>
/// A message that represents a status change in the application meant to be shown to users on the UI.
/// </summary>
public class AppStatusMessage(
    string text,
    AppStatusMessageKind kind = AppStatusMessageKind.Notice,
    AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info)
{
    public AppStatusMessage(
        Guid scriptId,
        string text,
        AppStatusMessageKind kind = AppStatusMessageKind.Notice,
        AppStatusMessageSeverity severity = AppStatusMessageSeverity.Info
    ) : this(text, kind, severity)
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
    /// The semantic kind of this message. See <see cref="AppStatusMessageKind"/>.
    /// </summary>
    public AppStatusMessageKind Kind { get; } = kind;

    /// <summary>
    /// The severity of this message. See <see cref="AppStatusMessageSeverity"/>.
    /// </summary>
    public AppStatusMessageSeverity Severity { get; } = severity;

    /// <summary>
    /// The date and time when this message was created.
    /// </summary>
    public DateTime CreatedDate { get; } = DateTime.Now;
}
