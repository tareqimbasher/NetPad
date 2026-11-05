using NetPad.Scripts;

namespace NetPad.Exceptions;

public class UnsupportedScriptKindException(ScriptKind kind)
    : Exception($"{kind} scripts aren't supported. Change the script's kind to C# Program to run it.");
