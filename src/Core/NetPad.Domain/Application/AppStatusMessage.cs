using System;

namespace NetPad.Application;

/// <summary>
/// Represents a status change in the application.
/// </summary>
public class AppStatusMessage
{
    public AppStatusMessage(string text, AppStatusMessagePriority priority = AppStatusMessagePriority.Normal, bool persistant = false)
    {
        Text = text;
        Priority = priority;
        Persistant = persistant;
        CreatedDate = DateTime.Now;
    }

    /// <summary>
    /// The text of this message.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The priority of this message.
    /// </summary>
    public AppStatusMessagePriority Priority { get; }

    /// <summary>
    /// Whether this status message should be persistant or it should clear out after a timeout.
    /// </summary>
    public bool Persistant { get; }

    /// <summary>
    /// The DateTime of when this message was created.
    /// </summary>
    public DateTime CreatedDate { get; }
}

public enum AppStatusMessagePriority
{
    Normal = 1, High = 2
}
