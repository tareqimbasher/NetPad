using NetPad.Scripts;

namespace NetPad.Exceptions;

public class UnsupportedScriptKindException(ScriptKind kind)
    : CannotRunScriptException($"{kind} scripts aren't supported. Change the script's kind to C# Program to run it.");
