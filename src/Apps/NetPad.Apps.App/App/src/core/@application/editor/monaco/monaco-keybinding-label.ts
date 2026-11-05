import {KeybindingCaps} from "@application/keybindings/keybinding-caps";

/**
 * Converts Monaco's label for a key combination ("Ctrl+K Ctrl+F") to {@link KeybindingCaps}. macOS
 * labels have no `+` ("⌘K ⌘F"), so each stroke comes back as a single cap.
 */
export function parseMonacoKeybindingLabel(label: string): KeybindingCaps {
    return label
        .split(" ")
        .filter(stroke => !!stroke)
        .map(stroke => stroke.split("+").filter(key => !!key));
}
