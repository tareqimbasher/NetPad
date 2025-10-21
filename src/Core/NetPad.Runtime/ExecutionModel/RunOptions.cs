using System.Diagnostics.CodeAnalysis;

namespace NetPad.ExecutionModel;

/// <summary>
/// Represents a set of options that control how a script is executed.
/// </summary>
/// <remarks>
/// <see cref="RunOptions"/> can hold arbitrary option objects, each identified by their type,
/// allowing scripts to be configured with context-specific settings at runtime.
/// </remarks>
public class RunOptions(string? specificCodeToRun = null)
{
    private readonly Dictionary<Type, object> _byType = new();

    /// <summary>
    /// Gets or sets a snippet of code to run instead of the full script.
    /// </summary>
    /// <remarks>
    /// When not <see langword="null"/>, this value overrides the script's main code during execution.
    /// It is typically used to execute only the portion of code that a user has highlighted in an editor.
    /// </remarks>
    public string? SpecificCodeToRun { get; set; } = specificCodeToRun;

    /// <summary>
    /// Stores an options object of type <typeparamref name="TOptions"/> in the current <see cref="RunOptions"/> instance.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options object to store.</typeparam>
    /// <param name="options">The options instance to associate with <typeparamref name="TOptions"/>.</param>
    /// <returns>The same <see cref="RunOptions"/> instance, allowing calls to be chained.</returns>
    /// <remarks>
    /// If an options object of the same type has already been added, it will be replaced.
    /// </remarks>
    public RunOptions SetOption<TOptions>(TOptions options) where TOptions : class
    {
        _byType[typeof(TOptions)] = options;
        return this;
    }

    /// <summary>
    /// Attempts to retrieve an options object of type <typeparamref name="TOptions"/> from the current
    /// <see cref="RunOptions"/> instance.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options object to retrieve.</typeparam>
    /// <param name="options">
    /// When this method returns <see langword="true"/>, contains the retrieved options instance;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an options object of the specified type was found; otherwise, <see langword="false"/>.
    /// </returns>
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
