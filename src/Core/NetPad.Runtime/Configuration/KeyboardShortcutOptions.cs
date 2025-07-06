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

public class KeyboardShortcutConfiguration(string id)
{
    public string Id { get; set; } = id;
    public bool Meta { get; set; }
    public bool Alt { get; set; }
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public KeyCode? Key { get; set; }
}

public enum KeyCode {
    Unknown = 0,
    Backspace = 1,
    Tab,
    Enter,
    ShiftLeft,
    ShiftRight,
    ControlLeft,
    ControlRight,
    AltLeft,
    AltRight,
    Pause,
    CapsLock,
    Escape,
    Space,
    PageUp,
    PageDown,
    End,
    Home,
    ArrowLeft,
    ArrowUp,
    ArrowRight,
    ArrowDown,
    PrintScreen,
    Insert,
    Delete,
    Digit0,
    Digit1,
    Digit2,
    Digit3,
    Digit4,
    Digit5,
    Digit6,
    Digit7,
    Digit8,
    Digit9,
    KeyA,
    KeyB,
    KeyC,
    KeyD,
    KeyE,
    KeyF,
    KeyG,
    KeyH,
    KeyI,
    KeyJ,
    KeyK,
    KeyL,
    KeyM,
    KeyN,
    KeyO,
    KeyP,
    KeyQ,
    KeyR,
    KeyS,
    KeyT,
    KeyU,
    KeyV,
    KeyW,
    KeyX,
    KeyY,
    KeyZ,
    MetaLeft,
    MetaRight,
    ContextMenu,
    Numpad0,
    Numpad1,
    Numpad2,
    Numpad3,
    Numpad4,
    Numpad5,
    Numpad6,
    Numpad7,
    Numpad8,
    Numpad9,
    NumpadMultiply,
    NumpadAdd,
    NumpadSubtract,
    NumpadDecimal,
    NumpadDivide,
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
    NumLock,
    ScrollLock,
    Semicolon,
    Equal,
    Comma,
    Minus,
    Period,
    Slash,
    Backquote,
    BracketLeft,
    Backslash,
    BracketRight,
    Quote,
}
