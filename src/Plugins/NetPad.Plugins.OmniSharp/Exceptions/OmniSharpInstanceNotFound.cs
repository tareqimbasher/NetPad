namespace NetPad.Plugins.OmniSharp.Exceptions;

public class OmniSharpInstanceNotFound(Guid scriptId) : Exception(
    $"Could not find a {nameof(AppOmniSharpServer)} for script '{scriptId}'"
);
