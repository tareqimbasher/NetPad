namespace NetPad.DotNet;

/// <summary>
/// The result of invoking the dotnet CLI.
/// </summary>
public record DotNetCliResult(bool Succeeded, string Output, string? Error = null)
{
    public string FormattedOutput
    {
        get
        {
            var hasOutput = !string.IsNullOrWhiteSpace(Output);
            var hasError = !string.IsNullOrWhiteSpace(Error);

            if (hasOutput && !hasError)
            {
                return $"Output: {Output}";
            }

            if (!hasOutput && hasError)
            {
                return $"Error: {Error}";
            }

            return $"Output: {Output}\nError: {Error}";
        }
    }
}
