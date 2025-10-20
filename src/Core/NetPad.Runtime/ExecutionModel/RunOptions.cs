using System.Diagnostics.CodeAnalysis;

namespace NetPad.ExecutionModel;

/// <summary>
/// Options that configure the running of a script.
/// </summary>
public class RunOptions(string? specificCodeToRun = null)
{
    /// <summary>
    /// If not null, this code will run instead of script code. Typically used to only run code that user has
    /// highlighted in the editor.
    /// </summary>
    public string? SpecificCodeToRun { get; set; } = specificCodeToRun;


    private readonly Dictionary<Type, object> _byType = new();

    public RunOptions Set<TOptions>(TOptions options) where TOptions : class
    {
        _byType[typeof(TOptions)] = options;
        return this;
    }

    public bool TryGet<TOptions>([NotNullWhen(true)] out TOptions? options) where TOptions : class
    {
        if (_byType.TryGetValue(typeof(TOptions), out var opts))
        {
            options = (TOptions)opts;
            return true;
        }

        options = null;
        return false;
    }
}
