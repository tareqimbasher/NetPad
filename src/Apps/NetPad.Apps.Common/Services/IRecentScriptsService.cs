namespace NetPad.Apps.Services;

/// <summary>
/// Tracks the most recently opened script paths.
/// </summary>
public interface IRecentScriptsService
{
    /// <summary>
    /// Gets the list of recent script paths, most-recent first. The result is not filtered for existence on disk;
    /// callers that need only existing files should filter the result themselves.
    /// </summary>
    IReadOnlyList<string> Get();

    /// <summary>
    /// Adds <paramref name="path"/> to the front of the recent list. Existing entries for the same path are
    /// removed. The list is capped at the maximum length.
    /// </summary>
    void Add(string path);

    /// <summary>
    /// Removes <paramref name="path"/> from the recent list. No-op if the path is not present.
    /// </summary>
    void Remove(string path);

    /// <summary>
    /// Clears the recent list.
    /// </summary>
    void Clear();
}
