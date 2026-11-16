namespace NetPad.Exceptions;

/// <summary>
/// Thrown when a script cannot be run for a reason the user can fix. The message is shown to the user as is.
/// </summary>
public class CannotRunScriptException(string message) : Exception(message);
