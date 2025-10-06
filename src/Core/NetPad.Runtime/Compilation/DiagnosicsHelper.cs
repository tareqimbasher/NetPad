using Microsoft.CodeAnalysis;

namespace NetPad.Compilation;

public static class DiagnosicsHelper
{
    /// <summary>
    /// Changes the line number of the diagnostic message by subtracting the specified value from the line number.
    /// </summary>
    public static string ReduceStacktraceLineNumbers(Diagnostic diagnostic, int subtract)
    {
        var err = diagnostic.ToString();

        if (!err.StartsWith('('))
        {
            return err;
        }

        var errParts = err.Split(':');
        var span = errParts.First().Trim(['(', ')']);
        var spanParts = span.Split(',');
        var lineNumberStr = spanParts[0];

        return int.TryParse(lineNumberStr, out int lineNumber)
            ? $"({lineNumber - subtract},{spanParts[1]}):{errParts.Skip(1).JoinToString(":")}"
            : err;
    }
}
