import * as monaco from "monaco-editor";
import {LogicalKey} from "@common";
import {KeyCombo} from "@application/keybindings/key-combo";

const namedKeys: Record<string, monaco.KeyCode> = {
    Backspace: monaco.KeyCode.Backspace,
    Tab: monaco.KeyCode.Tab,
    Enter: monaco.KeyCode.Enter,
    Escape: monaco.KeyCode.Escape,
    Space: monaco.KeyCode.Space,
    PageUp: monaco.KeyCode.PageUp,
    PageDown: monaco.KeyCode.PageDown,
    End: monaco.KeyCode.End,
    Home: monaco.KeyCode.Home,
    ArrowLeft: monaco.KeyCode.LeftArrow,
    ArrowUp: monaco.KeyCode.UpArrow,
    ArrowRight: monaco.KeyCode.RightArrow,
    ArrowDown: monaco.KeyCode.DownArrow,
    Insert: monaco.KeyCode.Insert,
    Delete: monaco.KeyCode.Delete,
    ContextMenu: monaco.KeyCode.ContextMenu,
    ";": monaco.KeyCode.Semicolon,
    "=": monaco.KeyCode.Equal,
    ",": monaco.KeyCode.Comma,
    "-": monaco.KeyCode.Minus,
    ".": monaco.KeyCode.Period,
    "/": monaco.KeyCode.Slash,
    "`": monaco.KeyCode.Backquote,
    "[": monaco.KeyCode.BracketLeft,
    "\\": monaco.KeyCode.Backslash,
    "]": monaco.KeyCode.BracketRight,
    "'": monaco.KeyCode.Quote,
};

/**
 * The Monaco key code a {@link LogicalKey} stands for, or undefined for keys Monaco has no code for.
 */
export function toMonacoKeyCode(key: LogicalKey): monaco.KeyCode | undefined {
    if (/^[A-Z]$/.test(key)) return monaco.KeyCode[`Key${key}` as keyof typeof monaco.KeyCode] as monaco.KeyCode;
    if (/^[0-9]$/.test(key)) return monaco.KeyCode[`Digit${key}` as keyof typeof monaco.KeyCode] as monaco.KeyCode;
    if (/^F([1-9]|1[0-9]|2[0-4])$/.test(key)) return monaco.KeyCode[key as keyof typeof monaco.KeyCode] as monaco.KeyCode;

    return namedKeys[key];
}

/**
 * The Monaco keybinding number a key combination stands for, or undefined when the combination is
 * unassigned or uses a key Monaco cannot express.
 */
export function toMonacoKeybinding(keyCombo: KeyCombo): number | undefined {
    if (!keyCombo.key) return undefined;

    const keyCode = toMonacoKeyCode(keyCombo.key);
    if (keyCode === undefined) return undefined;

    let keybinding = keyCode as number;
    if (keyCombo.primary) keybinding |= monaco.KeyMod.CtrlCmd;
    if (keyCombo.shift) keybinding |= monaco.KeyMod.Shift;
    if (keyCombo.alt) keybinding |= monaco.KeyMod.Alt;
    if (keyCombo.meta) keybinding |= monaco.KeyMod.WinCtrl;

    return keybinding;
}
