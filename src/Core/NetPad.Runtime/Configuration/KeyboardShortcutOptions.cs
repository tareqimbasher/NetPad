using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class KeyboardShortcutOptions : ISettingsOptions
{
    public KeyboardShortcutOptions()
    {
        DefaultMissingValues();
    }

    [JsonInclude] public List<KeyboardShortcutConfiguration> Shortcuts { get; private set; } = null!;

    public KeyboardShortcutOptions SetShortcuts(IList<KeyboardShortcutConfiguration> shortcuts)
    {
        if (shortcuts.Any(s => string.IsNullOrWhiteSpace(s.Id)))
        {
            throw new ArgumentException("One or more shortcuts does not have an Id");
        }

        if (shortcuts.GroupBy(s => s.Id).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("Some shortcuts had duplicate Ids");
        }

        Shortcuts = shortcuts.ToList();

        return this;
    }

    public void DefaultMissingValues()
    {
        Shortcuts = (Shortcuts ??= [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .DistinctBy(s => s.Id)
            .ToList();
    }
}

/// <summary>
/// A key combination assigned to a command.
/// </summary>
public class KeyboardShortcutConfiguration(string id)
{
    /// <summary>
    /// The id of the command the combination runs.
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// Whether the platform's primary modifier is part of the combination: Ctrl on Windows and
    /// Linux, Cmd on macOS.
    /// </summary>
    public bool Primary { get; set; }

    /// <summary>
    /// Whether the platform's other system modifier is part of the combination: Meta/Super on
    /// Windows and Linux, Ctrl on macOS.
    /// </summary>
    public bool Meta { get; set; }

    public bool Alt { get; set; }
    public bool Shift { get; set; }

    /// <summary>
    /// The key the combination ends on, named by what it produces on the user's layout rather than
    /// by its position: a single upper-cased character ("S", "="), or a key name ("F5", "Tab").
    /// </summary>
    public string? Key { get; set; }
}
