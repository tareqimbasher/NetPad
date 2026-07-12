namespace NetPad.Application;

/// <summary>
/// The semantic kind of an <see cref="AppStatusMessage"/>: how long the message stays relevant and
/// how much attention it demands. The UI derives how a message is surfaced from its kind.
/// </summary>
public enum AppStatusMessageKind
{
    /// <summary>
    /// Relevant only in the moment (e.g. "Compiling..."). If the user misses it, nothing is lost; not kept on record.
    /// </summary>
    Transient = 1,

    /// <summary>
    /// A noteworthy occurrence kept on record for the user to review later (e.g. "Script finished with an error").
    /// Does not demand immediate attention.
    /// </summary>
    Notice = 2,

    /// <summary>
    /// Something the user must become aware of (e.g. a data-loss warning).
    /// </summary>
    Alert = 3
}
