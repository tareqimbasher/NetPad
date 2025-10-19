using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NetPad.Scripts;

namespace NetPad.Apps.Cli;

public static class ScriptFinder
{
    public static readonly ImmutableArray<string> AutoFindFileExtensions =
    [
        Script.STANDARD_EXTENSION,
        ".cs"
    ];

    private static readonly EnumerationOptions _scriptEnumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive
    };

    /// <summary>
    /// Finds scripts that match a path or name query.
    /// </summary>
    /// <param name="searchDirectory">The directory from which to start the search. The search is recursive.</param>
    /// <param name="pathOrName">
    /// An absolute or relative path to a script, or a name to match against script
    /// files and their directories.</param>
    /// <returns>Full paths to script files that match the query.</returns>
    public static string[] FindMatches(string searchDirectory, string pathOrName)
    {
        if (TryResolveFilePath(pathOrName, out var path))
        {
            return [path];
        }

        var name = pathOrName;

        IEnumerable<string> files = [];
        foreach (var extension in AutoFindFileExtensions)
        {
            files = files.Concat(Directory.EnumerateFiles(searchDirectory, $"*{extension}", _scriptEnumerationOptions));
        }

        var matches = files
            .Select(fullPath => new
            {
                FullPath = fullPath,
                RelativePath = Path.GetRelativePath(searchDirectory, fullPath),
                DirectoryName = Path.GetDirectoryName(fullPath),
                FileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath)
            })
            .Where(p => p.RelativePath.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            return [];
        }

        if (matches.Length == 1)
        {
            return matches.Select(x => x.FullPath).ToArray();
        }

        var bestMatchesFirst = matches
            // Exact equality first
            .OrderByDescending(x => x.FileNameWithoutExtension.Equals(name, StringComparison.OrdinalIgnoreCase))
            // Then starts with
            .ThenByDescending(x => x.FileNameWithoutExtension.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            // Then if exactly matches a directory in path
            .ThenByDescending(x => x.DirectoryName?
                .Split(Path.DirectorySeparatorChar)
                .Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(x => x.DirectoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.FileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return bestMatchesFirst.Select(x => x.FullPath).ToArray();
    }

    private static bool TryResolveFilePath(string text, [NotNullWhen(true)] out string? path)
    {
        if (File.Exists(text))
        {
            path = text;
            return true;
        }

        path = Path.GetFullPath(text);
        if (File.Exists(path))
        {
            return true;
        }

        path = null;
        return false;
    }
}
