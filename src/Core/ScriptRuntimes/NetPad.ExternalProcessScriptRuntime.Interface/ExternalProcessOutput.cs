namespace NetPad.Runtimes;

/// <summary>
/// A wrapper for all <see cref="ScriptOutput"/> emitted by external process runtime.
/// </summary>
/// <param name="Type">The type name of the specified <see cref="ScriptOutput"/>.</param>
/// <param name="Output">The wrapped <see cref="ScriptOutput"/>.</param>
public record ExternalProcessOutput(string Type, ScriptOutput? Output);
