namespace NetPad.Runtimes;

internal interface IExternalProcessOutputWriter
{
    System.Threading.Tasks.Task WriteResultAsync(object? output, string? title = null, bool appendNewLine = false);
    System.Threading.Tasks.Task WriteSqlAsync(object? output, string? title = null);
}
